package com.example.echo;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Context;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

public class MainActivity extends AppCompatActivity implements AccessoryIO.Listener {
    private AccessoryIO mAccessoryIO;
    private EditText mMessageEditText;
    private Button mSendButton;
    private TextView mEchoTextView;


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        mMessageEditText = findViewById(R.id.message);
        mSendButton = findViewById(R.id.send);
        mEchoTextView = findViewById(R.id.echo);

        mSendButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if(mAccessoryIO.isConnected()) {
                    mAccessoryIO.write(mMessageEditText.getText().toString());
                } else {
                    Toast.makeText(MainActivity.this,
                            "Accessory not connected",
                            Toast.LENGTH_LONG).show();

                    mMessageEditText.setText("");
                }
            }
        });

        mAccessoryIO = new AccessoryIO(this, this);

    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        mAccessoryIO = null;
    }

    @Override
    public void onRead(final String data) {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                mEchoTextView.setText(data);
            }
        });
    }

    @Override
    public void onShutdown() {
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                finish();
            }
        });
    }
}