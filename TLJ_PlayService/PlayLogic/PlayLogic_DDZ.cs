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

class PlayLogic_DDZ: DDZ_GameBase
{
    static PlayLogic_DDZ s_playLogic = null;

    List<DDZ_RoomData> m_roomList = new List<DDZ_RoomData>();

    string m_tag = TLJCommon.Consts.GameRoomType_DDZ_Normal;        // "DDZ_Normal"
    string m_logFlag = "PlayLogic_DDZ";

    public static PlayLogic_DDZ getInstance()
    {
        if (s_playLogic == null)
        {
            s_playLogic = new PlayLogic_DDZ();
        }

        return s_playLogic;
    }

    public int getPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < m_roomList.Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (!playerDataList[j].isOffLine())
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
                case (int) TLJCommon.Consts.DDZ_PlayAction.PlayAction_JoinGame:
                {
                    doTask_JoinGame(connId, data);
                }
                break;

                case (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_ExitGame:
                {
                    DDZ_GameLogic.doTask_ExitGame(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_QiangDiZhu:
                {
                    DDZ_GameLogic.doTask_QiangDiZhu(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_JiaBang:
                {
                    DDZ_GameLogic.doTask_JiaBang(this, connId, data);
                }
                break;

                case (int) TLJCommon.Consts.DDZ_PlayAction.PlayAction_PlayerOutPoker:
                {
                    DDZ_GameLogic.doTask_ReceivePlayerOutPoker(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_Chat:
                {
                    DDZ_GameLogic.doTask_Chat(this, connId, data);
                }
                break;

                case (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_SetTuoGuanState:
                {
                    DDZ_GameLogic.doTask_SetTuoGuanState(this, connId, data);
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

            DDZ_RoomData room = null;

            // 检测该玩家是否已经加入房间
            if (DDZ_GameUtil.checkPlayerIsInRoom(uid))
            { 
                // 给客户端回复
                {
                    JObject respondJO = new JObject();
                    respondJO.Add("tag", tag);
                    respondJO.Add("playAction", playAction);
                    respondJO.Add("gameRoomType", DDZ_GameUtil.getRoomByUid(uid).m_gameRoomType);
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
                    if ((gameroomtype.CompareTo(m_roomList[i].m_gameRoomType) == 0) && (m_roomList[i].getRoomState() == DDZ_RoomState.RoomState_waiting))
                    {
                        if (m_roomList[i].joinPlayer(new DDZ_PlayerData(connId, uid, false, gameroomtype)))
                        {
                            room = m_roomList[i];
                            break;
                        }
                    }
                }

                // 当前没有房间可加入的话则创建一个新的房间
                if (room == null)
                {
                    room = new DDZ_RoomData(this, gameroomtype);
                    room.joinPlayer(new DDZ_PlayerData(connId, uid, false, gameroomtype));

                    m_roomList.Add(room);

                    LogUtil.getInstance().writeRoomLog(room, "新建比赛场房间：" + room.getRoomId());
                }
            }

            // 加入房间成功，给客户端回复
            {
                JObject respondJO = new JObject();
                respondJO.Add("tag", tag);
                respondJO.Add("playAction", playAction);
                respondJO.Add("gameRoomType", gameroomtype);
                respondJO.Add("code", (int) TLJCommon.Consts.Code.Code_OK);
                respondJO.Add("roomId", room.getRoomId());

                // 发送给客户端
                PlayService.m_serverUtil.sendMessage(connId, respondJO.ToString());
            }

            // 检测房间人数是否可以开赛
            DDZ_GameLogic.checkRoomStartGame(room, tag);
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTask_JoinGame异常：" + ex);
        }
    }

    DDZ_RoomData getRoomByUid(string uid)
    {
        DDZ_RoomData room = null;

        // 先在休闲场里找
        for (int i = 0; i < m_roomList.Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = m_roomList[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = m_roomList[i];
                    return room;
                }
            }
        }

        return room;
    }

    //------------------------------------------------------------------以上内容休闲场和PVP逻辑一样--------------------------------------------------------------

    public override List<DDZ_RoomData> getRoomList()
    {
        return m_roomList;
    }

    public override string getTag()
    {
        return m_tag;
    }

    public override bool doTaskPlayerCloseConn(string uid)
    {
        try
        {
            DDZ_RoomData room = getRoomByUid(uid);
            if (room == null)
            {
                return false;
            }

            DDZ_PlayerData playerData = DDZ_GameUtil.getPlayerDataByUid(uid);
            if (playerData == null)
            {
                return false;
            }

            //// 记录逃跑数据
            //if ((m_roomList[i].m_roomState != RoomState.RoomState_waiting) &&
            //    (m_roomList[i].m_roomState != RoomState.RoomState_end))
            //{
            //    Request_RecordUserGameData.doRequest(room.getPlayerDataList()[i].m_uid, room.m_gameRoomType, (int)TLJCommon.Consts.GameAction.GameAction_Run);
            //}

            switch (room.getRoomState())
            {
                case DDZ_RoomState.RoomState_waiting:
                    {
                        if (!playerData.isOffLine())
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌满人之前退出：" + playerData.m_uid);

                            room.getPlayerDataList().Remove(playerData);

                            if (DDZ_GameUtil.checkRoomNonePlayer(room))
                            {
                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());

                                DDZ_GameLogic.removeRoom(this, room, true);
                            }
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerData.m_uid);
                        }
                    }
                    break;

                case DDZ_RoomState.RoomState_fapai:
                case DDZ_RoomState.RoomState_qiangdizhu:
                case DDZ_RoomState.RoomState_jiabang:
                case DDZ_RoomState.RoomState_gaming:
                    {
                        if (!playerData.isOffLine())
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在游戏中退出：" + playerData.m_uid);
                            playerData.setIsOffLine(true);
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerData.m_uid);
                        }
                    }
                    break;

                case DDZ_RoomState.RoomState_end:
                    {
                        if (!playerData.isOffLine())
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在本桌打完后退出：" + playerData.m_uid);

                            room.getPlayerDataList().Remove(playerData);
                            if (room.getPlayerDataList().Count == 0)
                            {
                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());

                                DDZ_GameLogic.removeRoom(this, room, true);
                            }
                            else
                            {
                                // 如果房间人数为空，则删除此房间
                                {
                                    if (DDZ_GameUtil.checkRoomNonePlayer(room))
                                    {
                                        LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());

                                        DDZ_GameLogic.removeRoom(this, room, true);
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerData.m_uid);
                        }
                    }
                    break;

                default:
                    {
                        if (!playerData.isOffLine())
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":玩家在未知阶段退出：" + playerData.m_uid);

                            room.getPlayerDataList().Remove(playerData);

                            if (DDZ_GameUtil.checkRoomNonePlayer(room))
                            {
                                LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此房间人数为0，解散房间：" + room.getRoomId());

                                DDZ_GameLogic.removeRoom(this, room, true);
                            }
                        }
                        else
                        {
                            LogUtil.getInstance().addDebugLog(m_logFlag + "----" + ":此玩家连续退出/掉线：" + playerData.m_uid);
                        }
                    }
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":doTaskPlayerCloseConn异常：" + ex);
        }

        return false;
    }

    // 游戏结束
    public override void gameOver(DDZ_RoomData room)
    {
        try
        {
            room.setRoomState(DDZ_RoomState.RoomState_end);

            LogUtil.getInstance().writeRoomLog(room, m_logFlag + "----" + ":比赛结束,roomid = :" + room.getRoomId());

            // 游戏在线统计
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                Request_OnlineStatistics.doRequest(room.getPlayerDataList()[i].m_uid, room.getRoomId(), room.m_gameRoomType, room.getPlayerDataList()[i].m_isAI, (int)Request_OnlineStatistics.OnlineStatisticsType.OnlineStatisticsType_exit);
            }

            // 是否春天
            {
                if (room.m_winPlayerData.m_isDiZhu == 1)
                {
                    bool isChunTian = true;
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isDiZhu == 0)
                        {
                            if (room.getPlayerDataList()[i].m_outPokerCiShu > 0)
                            {
                                isChunTian = false;
                                break;
                            }
                        }
                    }

                    if (isChunTian)
                    {
                        room.m_beishu_chuntian = 2;
                    }
                }
                else
                {
                    bool isChunTian = true;
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isDiZhu == 1)
                        {
                            if (room.getPlayerDataList()[i].m_outPokerCiShu > 1)
                            {
                                isChunTian = false;
                                break;
                            }
                        }
                    }

                    if (isChunTian)
                    {
                        room.m_beishu_chuntian = 2;
                    }
                }
            }

            // 计算每个玩家的金币（积分）
            DDZ_GameUtil.setPlayerScore(room, false);

            // 加减金币
            {
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 加、减玩家金币值
                    Request_ChangeUserWealth.doRequest(room.getPlayerDataList()[i].m_uid, 1, room.getPlayerDataList()[i].m_score, "斗地主结算");
                }
            }

            //// 逻辑处理
            //{
            //    List<string> winnerList = new List<string>();

            //    // 游戏数据统计
            //    Request_GameStatistics.doRequest(room, winnerList);
            //}

            {
                JObject respondJO = new JObject();
                {
                    respondJO.Add("tag", room.m_tag);
                    respondJO.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_GameOver);
                    respondJO.Add("isDiZhuWin", room.m_winPlayerData.m_isDiZhu);

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        JObject jo = new JObject();
                        jo.Add("score", room.getPlayerDataList()[i].m_score);

                        float beishu = room.m_maxJiaoFenPlayerData.m_jiaofen * room.m_beishu_chuntian * room.m_beishu_bomb;
                        jo.Add("beishu", (int)beishu * (room.getPlayerDataList()[i].m_isJiaBang + 1));

                        respondJO.Add(room.getPlayerDataList()[i].m_uid, jo);
                    }

                    // 倍数
                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        DDZ_PlayerData playerData = room.getPlayerDataList()[i];

                        JArray ja = new JArray();

                        {
                            ja.Add("初始倍数X" + room.m_maxJiaoFenPlayerData.m_jiaofen);

                            if (playerData.m_isJiaBang == 1)
                            {
                                ja.Add("加棒X2");
                            }

                            if (room.m_beishu_bomb > 1)
                            {
                                ja.Add("炸弹X" + room.m_beishu_bomb);
                            }

                            if (room.m_beishu_chuntian > 1)
                            {
                                ja.Add("春天X" + room.m_beishu_chuntian);
                            }
                        }

                        respondJO.Add("beishu_" + playerData.m_uid, ja);
                    }
                }

                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    // 推送给客户端
                    if (!room.getPlayerDataList()[i].isOffLine())
                    {
                        PlayService.m_serverUtil.sendMessage(room.getPlayerDataList()[i].m_connId, respondJO.ToString());
                    }
                }
            }

            DDZ_GameLogic.removeRoom(this, room, true);
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "gameOver异常: " + ex);
        }
    }
}