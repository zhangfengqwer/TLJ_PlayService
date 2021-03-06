﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TLJ_PlayService;
using TLJCommon;

class GameUtil
{
    static string m_logFlag = "GameUtil";

    // 找出一组牌中某种花色的单牌
    public static List<TLJCommon.PokerInfo> choiceSinglePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return singleList;
    }

    // 找出一组牌中某种花色的对子
    public static List<TLJCommon.PokerInfo> choiceDoublePoker(List<TLJCommon.PokerInfo> myPokerList, TLJCommon.Consts.PokerType pokerType)
    {
        // 先筛选出同花色的牌
        List<TLJCommon.PokerInfo> pokerList = new List<TLJCommon.PokerInfo>();
        for (int i = myPokerList.Count - 1; i >= 0; i--)
        {
            if (myPokerList[i].m_pokerType == pokerType)
            {
                pokerList.Add(myPokerList[i]);
            }
        }

        List<TLJCommon.PokerInfo> singleList = new List<TLJCommon.PokerInfo>();
        List<TLJCommon.PokerInfo> doubleList = new List<TLJCommon.PokerInfo>();

        if (pokerList.Count > 1)
        {
            for (int i = pokerList.Count - 1; i >= 1; i--)
            {
                if (pokerList[i].m_num == pokerList[i - 1].m_num)
                {
                    doubleList.Add(pokerList[i]);
                    --i;

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
                else
                {
                    singleList.Add(pokerList[i]);

                    if (i == 1)
                    {
                        singleList.Add(pokerList[i - 1]);
                    }
                }
            }
        }
        else if (pokerList.Count == 1)
        {
            singleList.Add(pokerList[0]);
        }

        return doubleList;
    }

    public static bool checkRoomNonePlayer(RoomData room)
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

    public static void clearRoomNonePlayer(RoomData room)
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
            RoomData tempRoom = PlayLogic_PVP.getInstance().getRoomList()[i];
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

    /*
     * canFuShu:是否可以为负数？
     * 休闲场score代表金币，是要扣除的，所以可以为负数
     * PVP场score代表积分，扣到0就不扣了，不能为负数
     */
    public static void setPlayerScore(RoomData room,bool canFuShu)
    {
        try
        {
            float jichufenshu = 985;
            float changcixishu = 1;
            float defenxishu = 1;
            float xianjiadefen;

            // 计算场次系数
            {
                List<string> tempList = new List<string>();
                CommonUtil.splitStr(room.m_gameRoomType, tempList, '_');

                switch (tempList[0])
                {
                    case "XiuXian":
                        {
                            if (tempList[2].CompareTo("Common") == 0)
                            {
                                changcixishu = 2;
                            }
                            else if (tempList[2].CompareTo("ChuJi") == 0)
                            {
                                changcixishu = 1;
                            }
                            else if (tempList[2].CompareTo("ZhongJi") == 0)
                            {
                                changcixishu = 2;
                            }
                            else if (tempList[2].CompareTo("GaoJi") == 0)
                            {
                                changcixishu = 3;
                            }
                        }
                        break;

                    case "PVP":
                        {
                            changcixishu = 1;
                        }
                        break;
                }
            }

            // 计算得分系数
            {
                if (room.m_getAllScore == 0)
                {
                    defenxishu = -3.6f;
                }
                else if ((room.m_getAllScore >= 5) && (room.m_getAllScore <= 40))
                {
                    defenxishu = -2.6f;
                }
                else if ((room.m_getAllScore >= 45) && (room.m_getAllScore <= 75))
                {
                    defenxishu = -1.6f;
                }
                else if (room.m_getAllScore == 80)
                {
                    defenxishu = 1.0f;
                }
                else if ((room.m_getAllScore >= 85) && (room.m_getAllScore <= 120))
                {
                    defenxishu = 1.6f;
                }
                else if ((room.m_getAllScore >= 125) && (room.m_getAllScore <= 195))
                {
                    defenxishu = 2.6f;
                }
                else if (room.m_getAllScore >= 200)
                {
                    defenxishu = 3.6f;
                }
            }

            xianjiadefen = jichufenshu * changcixishu * defenxishu;

            if (!canFuShu)
            {
                // 闲家赢
                if (xianjiadefen > 0)
                {
                    float winerCanGetScote = 0;

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 1)
                        {
                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(room.getPlayerDataList()[i].m_uid);
                            if (userInfo_Game != null)
                            {
                                if (userInfo_Game.gold >= xianjiadefen)
                                {
                                    winerCanGetScote += xianjiadefen;
                                    room.getPlayerDataList()[i].m_score = (int)(-xianjiadefen);
                                }
                                else
                                {
                                    winerCanGetScote += userInfo_Game.gold;
                                    room.getPlayerDataList()[i].m_score = (-userInfo_Game.gold);
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPlayerScore出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }
                        }
                    }

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker != 1)
                        {
                            room.getPlayerDataList()[i].m_score = (int)winerCanGetScote / 2;
                        }
                    }
                }
                // 庄家赢
                else
                {
                    float winerCanGetScote = 0;

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(room.getPlayerDataList()[i].m_uid);
                        if (userInfo_Game != null)
                        {
                            if (room.getPlayerDataList()[i].m_isBanker != 1)
                            {
                                if (userInfo_Game.gold >= (-xianjiadefen))
                                {
                                    winerCanGetScote += (-xianjiadefen);
                                    room.getPlayerDataList()[i].m_score = (int)xianjiadefen;
                                }
                                else
                                {
                                    winerCanGetScote += userInfo_Game.gold;
                                    room.getPlayerDataList()[i].m_score = (-userInfo_Game.gold);
                                }
                            }
                        }
                        else
                        {
                            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPlayerScore出错：UserInfo_Game_Manager.getDataByUid()为空");
                        }
                    }

                    for (int i = 0; i < room.getPlayerDataList().Count; i++)
                    {
                        if (room.getPlayerDataList()[i].m_isBanker == 1)
                        {
                            room.getPlayerDataList()[i].m_score = (int)winerCanGetScote / 2;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < room.getPlayerDataList().Count; i++)
                {
                    if (room.getPlayerDataList()[i].m_isBanker == 1)
                    {
                        room.getPlayerDataList()[i].m_score += (-(int)xianjiadefen);
                    }
                    else
                    {
                        room.getPlayerDataList()[i].m_score += (int)xianjiadefen;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error("GameUtil.setPlayerScore()----" + ex.Message + "gameRoomType:" + room.m_gameRoomType);
        }
    }

    public static void setPVPReward(PVPRoomPlayerList curPVPRoomPlayerList)
    {
        string gameRoomType = curPVPRoomPlayerList.m_gameRoomType;

        List<string> list = new List<string>();
        CommonUtil.splitStr(gameRoomType, list, '_');

        if (list.Count == 3)
        {
            if (list[0].CompareTo("PVP") == 0)
            {
                PVPGameRoomData pvpGameRoomData = PVPGameRoomDataScript.getInstance().getDataByRoomType(gameRoomType);

                // PVP-金币场
                if (list[1].CompareTo("JinBi") == 0)
                {
                    // 2000金币场
                    if (pvpGameRoomData.reward_num == 2000)
                    {
                        // 第一名
                        {
                            int getHuiZhangNum = 1;

                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(curPVPRoomPlayerList.m_playerList[0].m_uid);
                            if (userInfo_Game != null)
                            {
                                int vipLevel = userInfo_Game.vipLevel;

                                if ((vipLevel >= 2) && (vipLevel <= 4))
                                {
                                    getHuiZhangNum += 1;
                                }
                                else if ((vipLevel >= 5) && (vipLevel <= 7))
                                {
                                    getHuiZhangNum += 2;
                                }
                                else if ((vipLevel >= 8) && (vipLevel <= 10))
                                {
                                    getHuiZhangNum += 3;
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPVPReward出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }

                            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "1:2000;110:" + getHuiZhangNum;
                        }

                        // 第二名
                        {
                            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:1000";
                        }
                    }
                    // 10000金币场
                    else if (pvpGameRoomData.reward_num == 10000)
                    {
                        // 第一名
                        {
                            int getHuiZhangNum = 1;

                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(curPVPRoomPlayerList.m_playerList[0].m_uid);
                            if (userInfo_Game != null)
                            {
                                int vipLevel = userInfo_Game.vipLevel;

                                if ((vipLevel >= 2) && (vipLevel <= 4))
                                {
                                    getHuiZhangNum += 1;
                                }
                                else if ((vipLevel >= 5) && (vipLevel <= 7))
                                {
                                    getHuiZhangNum += 2;
                                }
                                else if ((vipLevel >= 8) && (vipLevel <= 10))
                                {
                                    getHuiZhangNum += 3;
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPVPReward出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }

                            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "1:10000;110:" + getHuiZhangNum;
                        }

                        // 第二名
                        {
                            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:1000";
                        }
                    }
                    // 5000金币场(用于ios审核)
                    else if (pvpGameRoomData.reward_num == 5000)
                    {
                        // 第一名
                        {
                            int getHuiZhangNum = 1;

                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(curPVPRoomPlayerList.m_playerList[0].m_uid);
                            if (userInfo_Game != null)
                            {
                                int vipLevel = userInfo_Game.vipLevel;

                                if ((vipLevel >= 2) && (vipLevel <= 4))
                                {
                                    getHuiZhangNum += 1;
                                }
                                else if ((vipLevel >= 5) && (vipLevel <= 7))
                                {
                                    getHuiZhangNum += 2;
                                }
                                else if ((vipLevel >= 8) && (vipLevel <= 10))
                                {
                                    getHuiZhangNum += 3;
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPVPReward出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }

                            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "1:5000;110:" + getHuiZhangNum;
                        }

                        // 第二名
                        {
                            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:1000";
                        }
                    }
                }
                // PVP-话费场
                else if (list[1].CompareTo("HuaFei") == 0)
                {
                    // 1元话费场
                    if (list[2].CompareTo("1") == 0)
                    {
                        // 第一名
                        {
                            int getHuiZhangNum = 1;

                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(curPVPRoomPlayerList.m_playerList[0].m_uid);
                            if (userInfo_Game != null)
                            {
                                int vipLevel = userInfo_Game.vipLevel;

                                if ((vipLevel >= 2) && (vipLevel <= 4))
                                {
                                    getHuiZhangNum += 1;
                                }
                                else if ((vipLevel >= 5) && (vipLevel <= 7))
                                {
                                    getHuiZhangNum += 2;
                                }
                                else if ((vipLevel >= 8) && (vipLevel <= 10))
                                {
                                    getHuiZhangNum += 3;
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPVPReward出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }

                            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "111:1;110:" + getHuiZhangNum;
                        }

                        // 第二名
                        {
                            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:500";
                        }
                    }
                    // 5元话费场
                    else if (list[2].CompareTo("5") == 0)
                    {
                        // 第一名
                        {
                            int getHuiZhangNum = 2;

                            UserInfo_Game userInfo_Game = UserInfo_Game_Manager.getDataByUid(curPVPRoomPlayerList.m_playerList[0].m_uid);
                            if (userInfo_Game != null)
                            {
                                int vipLevel = userInfo_Game.vipLevel;

                                if ((vipLevel >= 2) && (vipLevel <= 4))
                                {
                                    getHuiZhangNum += 1;
                                }
                                else if ((vipLevel >= 5) && (vipLevel <= 7))
                                {
                                    getHuiZhangNum += 2;
                                }
                                else if ((vipLevel >= 8) && (vipLevel <= 10))
                                {
                                    getHuiZhangNum += 3;
                                }
                            }
                            else
                            {
                                TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":setPVPReward出错：UserInfo_Game_Manager.getDataByUid()为空");
                            }

                            curPVPRoomPlayerList.m_playerList[0].m_pvpReward = "112:1;110:" + getHuiZhangNum;
                        }

                        // 第二名
                        {
                            curPVPRoomPlayerList.m_playerList[1].m_pvpReward = "1:5000;110:1";
                        }
                    }
                }
            }
        }
    }

    public static bool checkPlayerIsInRoom(string uid)
    {
        bool b = false;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count;  i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    b = true;
                    break;
                }
            }
        }

        // 然后在比赛场里找
        if (!b)
        {
            for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
            {
                List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

                for (int j = 0; j < playerDataList.Count; j++)
                {
                    if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                    {
                        b = true;
                        break;
                    }
                }
            }
        }

        return b;
    }

    public static RoomData getRoomByUid(string uid)
    {
        RoomData room = null;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = PlayLogic_Relax.getInstance().getRoomList()[i];
                    return room;
                }
            }
        }

        // 然后在比赛场里找
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    room = PlayLogic_PVP.getInstance().getRoomList()[i];
                    return room;
                }
            }
        }

        return room;
    }

    public static PlayerData getPlayerDataByUid(string uid)
    {
        PlayerData playerData = null;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_uid.CompareTo(uid) == 0)
                {
                    playerData = playerDataList[j];

                    return playerData;
                }
            }
        }

        // 然后在比赛场里找
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

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

    public static PlayerData getPlayerDataByConnId(IntPtr connId)
    {
        PlayerData playerData = null;

        // 先在休闲场里找
        for (int i = 0; i < PlayLogic_Relax.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_Relax.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_connId== connId)
                {
                    playerData = playerDataList[j];

                    return playerData;
                }
            }
        }

        // 然后在比赛场里找
        for (int i = 0; i < PlayLogic_PVP.getInstance().getRoomList().Count; i++)
        {
            List<PlayerData> playerDataList = PlayLogic_PVP.getInstance().getRoomList()[i].getPlayerDataList();

            for (int j = 0; j < playerDataList.Count; j++)
            {
                if (playerDataList[j].m_connId == connId)
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
}