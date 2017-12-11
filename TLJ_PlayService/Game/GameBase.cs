﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class GameBase
{
    public abstract List<RoomData> getRoomList();
    public abstract void gameOver(RoomData room, string data);
    public abstract bool doTaskPlayerCloseConn(IntPtr connId);
}