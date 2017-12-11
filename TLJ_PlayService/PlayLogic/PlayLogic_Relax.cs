﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJCommon;
using TLJ_PlayService;
using static TLJCommon.Consts;

class PlayLogic_Relax: GameBase
{
    static PlayLogic_Relax s_playLogic_Normal = null;

    List<RoomData> m_roomList = new List<RoomData>();

    string m_tag = TLJCommon.Consts.Tag_XiuXianChang;
    string m_logFlag = "PlayLogic_Relax";

    public static PlayLogic_Relax getInstance()
    {
        if (s_playLogic_Normal == null)
        {
            s_playLogic_Normal = new PlayLogic_Relax();
        }

        return s_playLogic_Normal;
    }

    public int getPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < m_roomList.Count; i++)
        {
            List<PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (!playerDataList[j].m_isOffLine)
                {
                    ++count;
                }
            }
        }
        return count;
    }

    public int getRoomCount()
    {
        return m_roomList.Count;
    }

    public void OnReceive(IntPtr connId, string data)
    {
        JObject respondJO = new JObject();

        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            respondJO.Add("tag", tag);

            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            switch (playAction)
            {
                case (int) TLJCommon.Consts.PlayAction.PlayAction_JoinGame:
                {
                    doTask_JoinGame(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_WaitMatchTimeOut:
                {
                    GameLogic.doTask_WaitMatchTimeOut(this,connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_ExitGame:
                {
                    GameLogic.doTask_ExitGame(this,connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_QiangZhu:
                {
                    GameLogic.doTask_QiangZhu(this,connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_MaiDi:
                {
                    GameLogic.doTask_MaiDi(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_PlayerChaoDi:
                {
                    GameLogic.doTask_PlayerChaoDi(this, connId, data);
                }
                break;

                case (int) TLJCommon.Consts.PlayAction.PlayAction_PlayerOutPoker:
                {
                    GameLogic.doTask_ReceivePlayerOutPoker(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGame:
                {
                    GameLogic.doTask_ContinueGame(this,connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_ChangeRoom:
                {
                    GameLogic.doTask_ChangeRoom(this,connId, data);
                }
                break;

                case (int)TLJCommon.Consts.PlayAction.PlayAction_Chat:
                {
                    GameLogic.doTask_Chat(this,connId, data);
                }
                break;
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().writeLogToLocalNow(m_logFlag + "----" + ".OnReceive()异常：" + ex.Message);
            // 客户端参数错误
            respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }

    void doTask_JoinGame(IntPtr connId, string data)
    {
        try
        {
            JObject jo = JObject.Parse(data);
            string tag = jo.GetValue("tag").ToString();
            string uid = jo.GetValue("uid").ToString();
            string gameroomtype = jo.GetValue("gameroomtype").ToString();
            int playAction = Convert.ToInt32(jo.GetValue("playAction"));

            RoomData room = null;

            // 检测该玩家是否已经加入房间
            if (GameUtil.checkPlayerIsInRoom(uid))
            { 
                // 给客户端回复
                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
                    respondJO.Add("playAction", playAction);
                    respondJO.Add("gameroomtype", gameroomtype);
                    respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_CommonFail);

                    // 发送给客户端
                    PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
                }

                return;
            }

            lock (m_roomList)
            {
                // 在已有的房间寻找可以加入的房间
                for (int i = 0; i < m_roomList.Count; i++)
                {
                    //if (gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0)
                    if ((gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0) && (1 == m_roomList[i].m_rounds_pvp) && (m_roomList[i].m_roomState == RoomState.RoomState_waiting))
                    {
                        if (m_roomList[i].joinPlayer(new PlayerData(connId, uid, false, gameroomtype)))
                        {
                            room = m_roomList[i];
                            break;
                        }
                    }
                }

                // 当前没有房间可加入的话则创建一个新的房间
                if (room == null)
                {
                    room = new RoomData(this, m_roomList.Count + 1, gameroomtype);
                    room.joinPlayer(new PlayerData(connId, uid, false, gameroomtype));

                    m_roomList.Add(room);

                    LogUtil.getInstance().addDebugLog("新建休闲场房间：" + room.getRoomId());
                }
            }

            // 加入房间成功，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("gameroomtype", gameroomtype);
                respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_OK);
                respondJO.Add("roomId", room.getRoomId());

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }

            // 检测房间人数是否可以开赛
            GameLogic.checkRoomStartGame(room,m_tag, true);
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex.Message);
        }
    }

    //------------------------------------------------------------------以上内容休闲场和PVP逻辑一样--------------------------------------------------------------

    public override List<RoomData> getRoomList()
    {
        return m_roomList;
    }

    public override bool doTaskPlayerCloseConn(IntPtr connId)
    {
        try
        {
            for (int i = 0; i < m_roomList.Count; i++)
            {
                RoomData room = m_roomList[i];
                List<PlayerData> playerDataList = room.getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_connId == connId)
                    {
                        //// 记录逃跑数据
                        //if ((m_roomList[i].m_roomState != RoomState.RoomState_waiting) &&
                        //    (m_roomList[i].m_roomState != RoomState.RoomState_end))
                        //{
                        //    Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_Run);
                        //}

                        switch (m_roomList[i].m_roomState)
                        {
                            case RoomState.RoomState_waiting:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌满人之前退出：" + playerDataList[j].m_uid);

                                        playerDataList.RemoveAt(j);

                                        GameUtil.checkAllOffLine(room);

                                        if (GameUtil.checkRoomNonePlayer(room))
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room);
                                            GameLogic.removeRoom(this, room);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_qiangzhu:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抢主阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        GameUtil.checkAllOffLine(room);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_zhuangjiamaidi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在庄家埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_MaiDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            //case RoomData.RoomState.RoomState_fanzhu:
                            //    {
                            //        LogUtil.getInstance().addDebugLog("玩家在反主阶段退出：" + playerDataList[j].m_uid);

                            //        playerDataList[j].m_isOffLine = true;
                            //    }
                            //    break;

                            case RoomState.RoomState_chaodi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在抄底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_ChaoDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_othermaidi:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在Other埋底阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList[j].m_isOffLine = true;

                                        GameUtil.checkAllOffLine(room);

                                        //TrusteeshipLogic.trusteeshipLogic_MaiDi(this, room, playerDataList[j]);
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_gaming:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在游戏中退出：" + playerDataList[j].m_uid);
                                        playerDataList[j].m_isOffLine = true;

                                        GameUtil.checkAllOffLine(room);

                                        // 如果当前房间正好轮到此人出牌
                                        if (m_roomList[i].m_curOutPokerPlayer.m_uid.CompareTo(playerDataList[j].m_uid) == 0)
                                        {
                                            //TrusteeshipLogic.trusteeshipLogic_OutPoker(this, m_roomList[i], playerDataList[j]);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            case RoomState.RoomState_end:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌打完后退出：" + playerDataList[j].m_uid);

                                        playerDataList.RemoveAt(j);
                                        if (playerDataList.Count == 0)
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + m_roomList[i].getRoomId());
                                            GameLogic.removeRoom(this, room);
                                        }
                                        else
                                        {
                                            // 检查此房间内是否有人想继续游戏，有的话则告诉他失败
                                            {
                                                for (int k = playerDataList.Count - 1; k >= 0; k--)
                                                {
                                                    if (playerDataList[k].m_isContinueGame)
                                                    {
                                                        {
                                                            JObject respondJO = new JObject();
                                                            respondJO.Add("tag", m_tag);
                                                            respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_ContinueGameFail);
                                                            respondJO.Add("uid", playerDataList[k].m_uid);

                                                            // 发送给客户端
                                                            PlayService.m_serverUtil.sendMessage(playerDataList[k].m_connId, respondJO.ToString());
                                                        }

                                                        playerDataList.RemoveAt(k);
                                                    }
                                                }
                                            }

                                            // 如果房间人数为空，则删除此房间
                                            {
                                                if (GameUtil.checkRoomNonePlayer(room))
                                                {
                                                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                                                    GameLogic.removeRoom(this, room);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;

                            default:
                                {
                                    if (!playerDataList[j].m_isOffLine)
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在未知阶段退出：" + playerDataList[j].m_uid);

                                        playerDataList.RemoveAt(j);

                                        if (GameUtil.checkRoomNonePlayer(room))
                                        {
                                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());
                                            GameLogic.removeRoom(this, room);
                                        }
                                    }
                                    else
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerDataList[j].m_uid);
                                    }
                                }
                                break;
                        }

                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":doTaskPlayerCloseConn异常：" + ex.Message);
        }

        return false;
    }

    // 游戏结束
    public override void gameOver(RoomData room, string data)
    {
        try
        {
            room.m_roomState = RoomState.RoomState_end;

            //LogUtil.getInstance().addDebugLog("比赛结束，解散该房间：" + room.getRoomId());
            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":比赛结束,roomid = :" + room.getRoomId());

            // 计算每个玩家的金币（积分）
            GameUtil.setPlayerScore(room,false);

            // 加减金币
            {
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 如果玩家这一局赢了，则检测是否有金币加倍buff？
                    if (room.getPlayerDataList()[i].m_score > 0)
                    {
                        bool canUse = false;
                        for (int j = 0; j < room.getPlayerDataList()[i].m_buffData.Count; j++)
                        {
                            if ((room.getPlayerDataList()[i].m_buffData[j].m_prop_id == (int)TLJCommon.Consts.Prop.Prop_jiabeika) && (room.getPlayerDataList()[i].m_buffData[j].m_buff_num > 0))
                            {
                                room.getPlayerDataList()[i].m_buffData[j].m_buff_num -= 1;
                                canUse = true;

                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家有双倍金币buff，金币奖励加倍 :" + room.getPlayerDataList()[i].m_uid);

                                break;
                            }
                        }

                        if (canUse)
                        {
                            room.getPlayerDataList()[i].m_score *= 2;

                            // 扣除玩家buff：加倍卡
                            Request_UseBuff.doRequest(room.getPlayerDataList()[i].m_uid, (int)TLJCommon.Consts.Prop.Prop_jiabeika);
                        }
                    }

                    // 加、减玩家金币值
                    Request_ChangeUserWealth.doRequest(room.getPlayerDataList()[i].m_uid, 1, room.getPlayerDataList()[i].m_score);
                }
            }

            // 逻辑处理
            {
                // 闲家赢
                if (room.m_getAllScore >= 80)
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 0)
                        {
                            ++room.getPlayerDataList()[i].m_myLevelPoker;
                            if (room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            room.m_levelPokerNum = room.getPlayerDataList()[i].m_myLevelPoker;

                            // 提交任务
                            if (!room.getPlayerDataList()[i].m_isAI)
                            {
                                Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 203);
                                Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 212);
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType,(int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }
                        }
                    }
                }
                // 庄家赢
                else
                {
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 1)
                        {
                            ++room.getPlayerDataList()[i].m_myLevelPoker;
                            if (room.getPlayerDataList()[i].m_myLevelPoker == 15)
                            {
                                room.getPlayerDataList()[i].m_myLevelPoker = 2;
                            }

                            room.m_levelPokerNum = room.getPlayerDataList()[i].m_myLevelPoker;

                            // 提交任务
                            if (!room.getPlayerDataList()[i].m_isAI)
                            {
                                Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 203);
                                Request_ProgressTask.doRequest(room.getPlayerDataList()[i].m_uid, 212);
                            }

                            // 记录胜利次数数据
                            {
                                Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType,(int)TLJCommon.Consts.GameAction.GameAction_Win);
                            }
                        }
                    }
                }
            }

            JObject jo = JObject.Parse(data);

            // 通知
            {
                JObject respondJO;
                {
                    respondJO = new JObject();

                    respondJO.Add("tag", m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.PlayAction.PlayAction_GameOver);
                    respondJO.Add("getAllScore", room.m_getAllScore);
                    respondJO.Add("isBankerWin", room.m_getAllScore >= 80 ? 0 : 1);
                    respondJO.Add("pre_uid", jo.GetValue("uid"));
                    respondJO.Add("pre_outPokerList", jo.GetValue("pokerList"));
                }

                // 给在线的人推送
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].m_isOffLine)
                    {
                        if (!(room.getPlayerDataList()[i].m_isAI))
                        {
                            if (respondJO.GetValue("score") != null)
                            {
                                respondJO.Remove("score");
                            }

                            respondJO.Add("score", room.getPlayerDataList()[i].m_score);

                            PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                        }
                    }
                    else
                    {
                        if (!(room.getPlayerDataList()[i].m_isAI))
                        {
                            // 记录逃跑数据
                            Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_Run);
                        }
                    }

                    if (room.getPlayerDataList()[i].m_isAI)
                    {
                        AIDataScript.getInstance().backOneAI(room.getPlayerDataList()[i].m_uid);
                    }

                    // 告诉数据库服务器该玩家打完一局
                    {
                        Request_GameOver.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType);
                    }
                }
            }

            // 检查是否删除该房间
            {
                // 先清理房间内离线的人和AI
                GameUtil.clearRoomNonePlayer(room);

                if (GameUtil.checkRoomNonePlayer(room))
                {
                    LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":所有人都离线，解散该房间：" + room.getRoomId());
                    GameLogic.removeRoom(this, room);
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog(m_logFlag + "----" + ":gameOver异常：" + ex.Message);
        }
    }
}