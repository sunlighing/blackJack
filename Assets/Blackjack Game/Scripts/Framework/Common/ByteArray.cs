using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ByteArray 
{
    public byte[] bytes; //缓冲区
    public int readIdx = 0; //读写位置
    public int writeIdx = 0; 

    public int length //数据长度
    {
        get
        {
            return writeIdx - readIdx;
        }
    }

    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        readIdx = 0;
        writeIdx = defaultBytes.Length;
    }

    public ByteArray()
    {
      //  bytes = defaultBytes;
     //   readIdx = 0;
      //  writeIdx = defaultBytes.Length;
    }
}
