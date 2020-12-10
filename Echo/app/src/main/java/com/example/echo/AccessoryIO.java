package com.example.echo;

import android.content.Context;
import android.hardware.usb.UsbAccessory;
import android.hardware.usb.UsbManager;
import android.os.Handler;
import android.os.Looper;
import android.os.Message;
import android.os.ParcelFileDescriptor;
import android.system.ErrnoException;
import android.system.OsConstants;
import android.util.Log;

import androidx.annotation.NonNull;

import java.io.Closeable;
import java.io.FileDescriptor;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;

public class AccessoryIO {
    public interface Listener {
        void onRead(String data);
        void onShutdown();
    }

    private static final String TAG = "Echo.AccessoryIO";
    private static final int CONNECT_POLL_WAIT = 100;
    private static final int READ_POLL_WAIT = 100;

    private Listener mListener;
    private UsbManager mUsbManager;
    private boolean mIsShutDown;
    private boolean mIsAttached;
    private FileOutputStream mOutputStream;
    private FileInputStream mInputStream;
    private ParcelFileDescriptor mParcelFileDescriptor;
    private ReadThread mReadThread;

    public AccessoryIO(final Context context, final Listener listener) {
        if(context == null || listener == null) {
            throw new AssertionError("Arguments context and listener cannot be null");
        }
        mListener = listener;
        mUsbManager = (UsbManager)context.getSystemService(Context.USB_SERVICE);
        mReadThread = new ReadThread();
        mReadThread.start();
    }

    public synchronized boolean write(String str) {
        if(mOutputStream == null) {
            throw new AssertionError("Can't write if shutdown or output stream is null");
        }

        byte[] stringData = str.getBytes();
        byte[] lengthData = getLengthBytes(stringData);

        try {
            mOutputStream.write(lengthData);
            mOutputStream.write(stringData);
            mOutputStream.flush();
            return true;
        } catch (IOException e) {
            if(ioExceptionIsNoSuchDevice(e)) {
                mReadThread.terminate();
                return false;
            }
            Log.d(TAG, "IOExceptiom white writing size+data bytes", e);
            return false;
        }
    }

    public boolean isConnected() {
        return mIsAttached;
    }

    private String read(final InputStream inputStream) throws IOException {
        final int bytesToRead;
        try {
            byte[] buffer = new byte[4];
            int count = inputStream.read(buffer, 0, 4);
            if(count != buffer.length) {
                Log.d(TAG, "Incorrect number of bytes read while reading size bytes: " +
                        "actual = " + count + " expected = " + buffer.length);
                return null;
            }
            bytesToRead = ByteBuffer.wrap(buffer).asIntBuffer().get();
        } catch (IOException exception) {
            if(ioExceptionIsNoSuchDevice(exception)) {
                throw exception;
            }
            Log.d(TAG, "IOException while reading size bytes", exception);
            return null;
        }

        try {
            byte[] buffer = new byte[bytesToRead];
            int bytesRead = inputStream.read(buffer, 0, bytesToRead);

            if(bytesRead != bytesToRead) {
                Log.d(TAG, "Incorrect number of bytes read while reading data bytes:"
                    + " actual =" + bytesRead + " expected =" + bytesToRead);
                return null;
            }

            return new String(buffer, 0, bytesToRead);
        } catch (IOException exception) {
            if(ioExceptionIsNoSuchDevice(exception)) {
                throw exception;
            }
            Log.d(TAG, "IOException while reading bytes", exception);
            return null;
        }
    }

    private byte[] getLengthBytes(byte[] data) {
        int length = data.length;
        byte[] bytes = ByteBuffer.allocate(4).putInt(length).array();
        return bytes;
    }

    private boolean ioExceptionIsNoSuchDevice(IOException ioException) {
        final Throwable cause = ioException.getCause();
        if (cause instanceof ErrnoException) {
            final ErrnoException errnoException = (ErrnoException) cause;
            return errnoException.errno == OsConstants.ENODEV;
        }
        return false;
    }

    private class ReadThread extends Thread {
        private static final int STOP_THREAD = 1;
        private static final int READ_DATA = 2;

        private Handler mHandler;

        @Override
        public void run() {
            Looper.prepare();

            mHandler = new Handler() {
                @Override
                public void handleMessage(@NonNull Message msg) {
                    switch (msg.what) {
                        case STOP_THREAD:
                            Looper.myLooper().quit();
                            break;
                        case READ_DATA:
                            Log.d(TAG, "Reading data");
                            String data;
                            try {
                                data = read(mInputStream);
                            }
                            catch (IOException exception) {
                                terminate();
                                break;
                            }

                            if(data != null || data.length() != 0) {
                                mListener.onRead(data);
                                mHandler.sendEmptyMessage(READ_DATA);
                            }
                            else {
                                mHandler.sendEmptyMessageDelayed(READ_DATA, READ_POLL_WAIT);
                            }
                            break;
                    }
                }
            };

            detectAccessory();

            Looper.loop();

            detachAccessory();
            mIsShutDown = true;
            mListener.onShutdown();

            mHandler = null;
            mListener = null;
            mUsbManager = null;
            mReadThread = null;
        }

        public void terminate() {
            mHandler.sendEmptyMessage(STOP_THREAD);
        }

        private void detectAccessory() {
            while (!mIsAttached) {
                if(mIsShutDown) {
                    mHandler.sendEmptyMessage(STOP_THREAD);
                    return;
                }

                try {
                    Thread.sleep(CONNECT_POLL_WAIT);
                }
                catch (InterruptedException e) {
                    // pass
                }

                final UsbAccessory[] accessories = mUsbManager.getAccessoryList();
                if(accessories == null || accessories.length == 0) {
                    continue;
                }

                if(accessories.length > 1) {
                    Log.w(TAG, "multiple accessories attached. Using the first one...");
                }

                attachAccessory(accessories[0]);
            }
        }

        private void attachAccessory(final UsbAccessory accessory) {
            Log.d(TAG, "Attaching accessory");
            Log.d(TAG, accessory.getDescription());
            final ParcelFileDescriptor parcelFileDescriptor = mUsbManager.openAccessory(accessory);
            Log.d(TAG, "Opened accessory");
            if(parcelFileDescriptor != null) {
                Log.d(TAG, "Opening endpoints");
                final FileDescriptor fd = parcelFileDescriptor.getFileDescriptor();
                mIsAttached = true;
                mOutputStream = new FileOutputStream(fd);
                mInputStream = new FileInputStream(fd);
                mParcelFileDescriptor = parcelFileDescriptor;
                Log.d(TAG, "Endpoints opened");
                mHandler.sendEmptyMessage(READ_DATA);
            }
        }

        private void detachAccessory() {
            if(mIsAttached) {
                mIsAttached = false;
            }

            if(mInputStream != null) {
                closeQuietly(mInputStream);
                mInputStream = null;
            }

            if(mOutputStream != null) {
                closeQuietly(mOutputStream);
                mOutputStream = null;
            }

            if(mParcelFileDescriptor != null) {
                closeQuietly(mParcelFileDescriptor);
                mParcelFileDescriptor = null;
            }
        }

        private void closeQuietly(Closeable closeable) {
            try {
                closeable.close();
            }
            catch (IOException ex) {
                // pass
            }
        }
    }
}