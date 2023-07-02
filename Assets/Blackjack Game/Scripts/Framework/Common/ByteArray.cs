using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ByteArray 
{
    const int DEFAULT_SIZE = 1024;
    int initSize = 0;
    public byte[] bytes; //缓冲区
    public int readIdx = 0; //读写位置
    public int writeIdx = 0; //容量
    private int capacity = 0; //剩余空间
    public int remain {get { return capacity - writeIdx; }} //数据长度

    public int length //数据长度
    {
        get
        {
            return writeIdx - readIdx;
        }
    }

    //构造函数
    public ByteArray(int size = DEFAULT_SIZE)
    {
        bytes = new byte[size];
        capacity = size;
        initSize = size;
        readIdx = 0;
        writeIdx = 0;
    }



    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        capacity = defaultBytes.Length;
        initSize = defaultBytes.Length;
        readIdx = 0;
        writeIdx = defaultBytes.Length;
    }

    public void ReSize(int size)
    {
        if (size < length) return;

        if (size < initSize) return;

        int n = 1;

        while (n < size) n *= 2;

        capacity = n;

        byte[] newBytes = new byte[capacity];

        Array.Copy(bytes, readIdx, newBytes, 0, writeIdx - readIdx);
        bytes = newBytes;
        writeIdx = length;
        readIdx = 0;
    }

    //检查并移动数据
    public void CheckAndMoveBytes()
    {
        if(length < 8)
        {
            MoveBytes();
        }
    }

    public void MoveBytes()
    {
        Array.Copy(bytes, readIdx, bytes, 0, length);
        writeIdx = length;
        readIdx = 0;
    }

    public ByteArray()
    {
      //  bytes = defaultBytes;
     //   readIdx = 0;
      //  writeIdx = defaultBytes.Length;
    }
}
