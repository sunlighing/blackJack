using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Linq;
using System;
using System.Security;

public enum NetEvent
{
    ConnectSucc =1,
    ConnectFail =2,
    Close = 3,
}

public static class NetworkManager 
{
    static Socket socket;

    static ByteArray readBuff;
    static Queue<ByteArray> writeQueue;

    static bool isConneciting = false;

    static bool isClosing = false;
    //事件委托类型
    public delegate void EventListener(string err); 

    //事件监听列表
    private static Dictionary<NetEvent, EventListener> eventListens = new Dictionary<NetEvent, EventListener>();

   //添加事件监听
   public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListens.ContainsKey(netEvent))
        {
            eventListens[netEvent] += listener;
        }
        else
        {
            eventListens[netEvent] = listener;
        }
    }

    //删除网络事件
    public static void RemoveEventListener(NetEvent netEvent,EventListener listener)
    {
        if (eventListens.ContainsKey(netEvent))
        {
            eventListens[netEvent] -= listener;
            eventListens.Remove(netEvent);
        }
    }

    //分发网络事件
    public static void FireEvent(NetEvent netEvent,string err)
    {
        if (eventListens.ContainsKey(netEvent))
        {
            eventListens[netEvent](err);
        }
    }

    //链接
    public static void Connect(string ip ,int port)
    {
        if(socket != null && socket.Connected)
        {
            Debug.LogError("链接失败，已经链接过了");
            return;
        }

        if (isConneciting)
        {
            Debug.Log("Connect fail,isConnnecting");
            return;
        }

        //初始化成员
        InitState();

        isConneciting = true;
        socket.BeginConnect(ip,port,ConnnectCallback,socket);
    }

    private static void InitState()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        readBuff = new ByteArray();
        writeQueue = new Queue<ByteArray>();
        isConneciting = false;
        isClosing = false;
    }

    //连接回调
    private static void ConnnectCallback( IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Success");
            FireEvent(NetEvent.ConnectSucc, "");
            isConneciting = false;

        }
        catch(SocketException ex)
        {
            Debug.Log("Socket Connect fail "+ex.ToString());
            FireEvent(NetEvent.ConnectFail, ex.ToString());
            isConneciting = false;
        }
    }

    //关闭连接
    public static void Close()
    {
        if (socket == null || !socket.Connected)
        {
            return;
        }

        if (isConneciting)
        {
            return;
        }

        if (writeQueue.Count > 0)
        {
            isClosing = true;
        }
        else
        {
            socket.Close();
            FireEvent(NetEvent.Close, "");
        }
    }

    public static void Send(int cmd, byte[] bodyByte)
    {
        if(socket == null || !socket.Connected)
        {
            return;
        }
        if (isConneciting)
        {
            return;
        }

        if (isClosing)
        {
            return;
        }

        byte[] nameBytes = BitConverter.GetBytes(cmd);
      
        int len = nameBytes.Length + bodyByte.Length;
        byte[] sendBytes = new byte[2 + len];
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyByte, 0, sendBytes, 2+nameBytes.Length, bodyByte.Length);

        //写入队列
        ByteArray ba = new ByteArray(sendBytes);
    }

    public static byte[] intToBytes(int value)
    {
        byte[] src = new byte[4];
        src[3] = (byte)((value >> 24) & 0xFF);
        src[2] = (byte)((value >> 16) & 0xFF);
        src[1] = (byte)((value >> 8) & 0xFF);
        src[0] = (byte)(value & 0xFF);
        return src;
    }
}
