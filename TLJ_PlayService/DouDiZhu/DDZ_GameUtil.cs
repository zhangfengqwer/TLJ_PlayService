﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;
using TLJCommon;

class DDZ_GameUtil
{
    static string m_logFlag = "DDZ_GameUtil";
    
    public static bool checkRoomNonePlayer(DDZ_RoomData room)
    {
        // 清理机器人和离线的人
        clearRoomNonePlayer(room);

        bool isRemove = true;
        if (room.getPlayerDataList().Count > 0)
        {
            isRemove = false;
        }

        return isRemove;
    }

    public static void clearRoomNonePlayer(DDZ_RoomData room)
    {
        // 删除机器人
        for (int i = room.getPlayerDataList().Count - 1; i >= 0; i--)
        {
            if (room.getPlayerDataList()[i].m_isAI)
            {
                LogUtil.getInstance().addDebugLog("清理机器人：" + room.getPlayerDataList()[i].m_uid);

                AIDataScript.getInstance().backOneAI(room.getPlayerDataList()[i].m_uid);
                room.getPlayerDataList().RemoveAt(i);
            }
        }

        // 删除离线的人
        for (int i = room.getPlayerDataList().Count - 1; i >= 0; i--)
        {
            if (room.getPlayerDataList()[i].isOffLine())
            {
                LogUtil.getInstance().addDebugLog("清理离线的人：" + room.getPlayerDataList()[i].m_uid);
                room.getPlayerDataList().RemoveAt(i);
            }
        }
    }

    public static int getGameRoomPlayerCount(string gameRoomType)
    {
        int count = 0;
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            DDZ_RoomData tempRoom = PlayLogic_DDZ.getInstance().getRoomList()[i];
            if (tempRoom.m_gameRoomType.CompareTo(gameRoomType) == 0)
            {
                for (int j = 0; j < tempRoom.getPlayerDataList().Count; j++)
                {
                    ++count;
                }
            }
        }

        return count;
    }
    
    public static bool checkPlayerIsInRoom(string uid)
    {
        bool b = false;
        
        for (int i = 0; i < PlayLogic_DDZ.getInstance().getRoomList().Count;  i++)
        {
            List<DDZ_PlayerData> playerDataList = PlayLogic_DDZ.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    b = true;
                    break;
                }
            }
        }

        return b;
    }

    public static DDZ_RoomData getRoomByUid(string uid)
    {
        DDZ_RoomData room = null;
        
        for (int i = 0; i < PlayLogic_DDZ.getInstance().getRoomList().Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = PlayLogic_DDZ.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = PlayLogic_DDZ.getInstance().getRoomList()[i];
                    return room;
                }
            }
        }

        return room;
    }

    public static DDZ_PlayerData getPlayerDataByUid(string uid)
    {
        DDZ_PlayerData playerData = null;
        
        for (int i = 0; i < PlayLogic_DDZ.getInstance().getRoomList().Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = PlayLogic_DDZ.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    playerData = playerDataList[j];

                    return playerData;
                }
            }
        }

        return playerData;
    }

    public static DDZ_PlayerData getPlayerDataByConnId(IntPtr connId)
    {
        DDZ_PlayerData playerData = null;
        
        for (int i = 0; i < PlayLogic_DDZ.getInstance().getRoomList().Count; i++)
        {
            List<DDZ_PlayerData> playerDataList = PlayLogic_DDZ.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_connId== connId)
                {
                    playerData = playerDataList[j];

                    return playerData;
                }
            }
        }

        return playerData;
    }

    public static string getRoomName(string gameRoomType)
    {
        string roonName = "";

        if (gameRoomType.CompareTo("XiuXian_JingDian_Common") == 0)
        {
            roonName = "经典玩法";
        }
        else if (gameRoomType.CompareTo("XiuXian_JingDian_ChuJi") == 0)
        {
            roonName = "经典玩法-新手场";
        }
        else if (gameRoomType.CompareTo("XiuXian_JingDian_ZhongJi") == 0)
        {
            roonName = "经典玩法-精英场";
        }
        else if (gameRoomType.CompareTo("XiuXian_JingDian_GaoJi") == 0)
        {
            roonName = "经典玩法-大师场";
        }
        else if (gameRoomType.CompareTo("XiuXian_ChaoDi_Common") == 0)
        {
            roonName = "抄底玩法";
        }
        else if (gameRoomType.CompareTo("XiuXian_ChaoDi_ChuJi") == 0)
        {
            roonName = "抄底玩法-新手场";
        }
        else if (gameRoomType.CompareTo("XiuXian_ChaoDi_ZhongJi") == 0)
        {
            roonName = "抄底玩法-精英场";
        }
        else if (gameRoomType.CompareTo("XiuXian_ChaoDi_GaoJi") == 0)
        {
            roonName = "抄底玩法-大师场";
        }
        else if (gameRoomType.CompareTo("PVP_JinBi_2000") == 0)
        {
            roonName = "2千金币场(抄底)";
        }
        else if (gameRoomType.CompareTo("PVP_JinBi_10000") == 0)
        {
            roonName = "1万金币场(抄底)";
        }
        else if (gameRoomType.CompareTo("PVP_HuaFei_1") == 0)
        {
            roonName = "1元话费场(抄底)";
        }
        else if (gameRoomType.CompareTo("PVP_HuaFei_5") == 0)
        {
            roonName = "5元话费场(抄底)";
        }
        else if (gameRoomType.CompareTo("PVP_JinBi_5000") == 0)
        {
            roonName = "5千金币场(抄底)";
        }

        return roonName;
    }

    public static void setPlayerScore(DDZ_RoomData room, bool canFuShu)
    {
        try
        {
            float jichufenshu = 100;
            float changcixishu = 1;
            float beishu = 1;
            
            for (int i = 0; i < room.getPlayerDataList().Count; i++)
            {
                float score = 0;

                // 叫分 * 春天倍数 * 炸弹倍数
                beishu = room.m_maxJiaoFenPlayerData.m_jiaofen * room.m_beishu_chuntian * room.m_beishu_bomb;
                if (room.getPlayerDataList()[i].m_isJiaBang == 1)
                {
                    beishu *= 2;
                }

                score = jichufenshu * changcixishu * beishu;
                room.getPlayerDataList()[i].m_score = (int)score;
            }

            // 地主赢
            if (room.m_winPlayerData.m_isDiZhu == 1)
            {
                int winerCanGetScote = 0;

                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    DDZ_PlayerData playerData = room.getPlayerDataList()[i];

                    if (playerData.m_isDiZhu != 1)
                    {
                        UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(playerData.m_uid);
                        if (userInfo_Game != null)
                        {
                            if (userInfo_Game.gold < playerData.m_score)
                            {
                                winerCanGetScote += userInfo_Game.gold;
                                playerData.m_score = -userInfo_Game.gold;
                            }
                            else
                            {
                                winerCanGetScote += playerData.m_score;
                                playerData.m_score = -playerData.m_score;
                            }
                        }
                    }
                }
                
                room.m_diZhuPlayer.m_score = winerCanGetScote;
            }
            // 农民赢
            else
            {
                int winerCanGetScote = 0;

                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    DDZ_PlayerData playerData = room.getPlayerDataList()[i];

                    if (playerData.m_isDiZhu != 1)
                    {
                        UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(playerData.m_uid);
                        if (userInfo_Game != null)
                        {
                            winerCanGetScote += playerData.m_score;
                        }
                    }
                }

                room.m_diZhuPlayer.m_score = -winerCanGetScote;
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("DDZ_GameUtil.setPlayerScore()----" + ex + "gameRoomType:" + room.m_gameRoomType);
        }
    }
}