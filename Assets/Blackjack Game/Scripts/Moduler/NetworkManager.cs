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

    public delegate void MsgListener(byte[] bytes);

    private static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();

    //收到消息列表
    static List<byte[]> msgList = new List<byte[]>();

    static int msgCount = 0;

    readonly static int MAX_MESSSAGE_FIRE = 10;

   
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
        msgList = new List<byte[]>();
        msgCount = 0;
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
            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback,socket);
        }
        catch(SocketException ex)
        {
            Debug.Log("Socket Connect fail "+ex.ToString());
            FireEvent(NetEvent.ConnectFail, ex.ToString());
            isConneciting = false;
        }
    }

    public static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndReceive(ar);
            if(count == 0)
            {
                Close();
                return;
            }
            readBuff.writeIdx += count;
            onReceiveData();
            if(readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.ReSize(readBuff.length * 2);
            }
            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }catch(SocketException ex)
        {
            Debug.Log("Socket Receive fail" + ex.ToString());
        }
    }

    //数据处理
    public static void onReceiveData()
    {
        if(readBuff.length <= 2)
        {
            return;
        }
        int readIdx = readBuff.readIdx;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if(readBuff.length < bodyLength)
        {
            return;
        }
        readBuff.readIdx += 2;
        int nameCount = 0;
      //  string protoN
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
        int count = 0;
        lock(writeQueue){
            writeQueue.Enqueue(ba);
            count = writeQueue.Count;
            if(count == 1)
            {
                socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
            }
        }
    }
    
    public static void SendCallback(IAsyncResult ar)
    {
        //获取state EndSend 的处理，
        Socket socket = (Socket)ar.AsyncState;
        //状态判断 
        if(socket == null || !socket.Connected)
        {
            return;
        }
        //EndSend
        int count = socket.EndSend(ar);

        //获取写入队列第一条数据
        ByteArray ba;
        lock (writeQueue)
        {
            ba = writeQueue.First();
        }

        ba.readIdx += count;

        if(ba.length == 0)
        {
            lock (writeQueue)
            {
                writeQueue.Dequeue();
                ba = writeQueue.First();

            }
        }

        //继续发送
        if(ba != null)
        {
            socket.BeginSend(ba.bytes, ba.readIdx, ba.length,0,SendCallback,socket);

        }else if(isClosing)
        {
            socket.Close();
        }

    }
    
    //增加消息监听事件
    public static void AddMsgListener(string msgName,MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] += listener;
        }
        else
        {
            msgListeners[msgName] = listener;
        }
    }

    //删除消息监听事件
    public static void RemoveListener(string msgName,MsgListener listener)
    {

        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] -= listener;

            if(msgListeners[msgName] == null)
            {
                msgListeners.Remove(msgName);
            }
        }
       
    }

    //消息分发
    private static void FireMsg(string msgName, byte[] buffer)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName](buffer);
        }
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

    public static void Update()
    {

    }

    public static void MsgUpdate()
    {
        if(msgCount == 0)
        {
            return;
        }

        for (int i =0; i< MAX_MESSSAGE_FIRE; i++)
        {
            byte[] msgBase = null;
            lock (msgList)
            {
                if(msgList.Count > 0)
                {
                    msgBase = msgList[0];
                    msgList.RemoveAt(0);
                    msgCount--;
                }
            }
            if(msgBase != null)
            {
                FireMsg("", msgBase);
            }
            else
            {
                break; 
            }
        }
    }
}
