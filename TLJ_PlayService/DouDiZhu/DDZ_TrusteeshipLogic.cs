﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CrazyLandlords.Helper;
using TLJCommon;

public class DDZ_TrusteeshipLogic
{
    static string m_logFlag = "DDZ_TrusteeshipLogic";

    // 托管:出牌
    public static void trusteeshipLogic_OutPoker(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            // 轮到自己出牌
            {
                if (playerData.getPokerList().Count > 0)
                {
                    JObject backData = new JObject();
                    backData.Add("tag", room.m_tag);
                    backData.Add("uid", playerData.m_uid);
                    backData.Add("playAction", (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_PlayerOutPoker);
                    {
                        List<TLJCommon.PokerInfo> listPoker = LandlordsCardsHelper.GetTrusteeshipPoker(room, playerData);

                        //// 打印托管出的牌
                        //{
                        //    string str = "";
                        //    for (int i = 0; i < listPoker.Count; i++)
                        //    {
                        //        str += (listPoker[i].m_num + "、");
                        //    }
                        //    TLJ_PlayService.PlayService.log.Warn(m_logFlag + "----托管出牌：" + playerData.m_uid + ":" + str);
                        //}

                        JArray jarray = new JArray();
                        for (int i = 0; i < listPoker.Count; i++)
                        {
                            int num = listPoker[i].m_num;
                            int pokerType = (int)listPoker[i].m_pokerType;
                            {
                                JObject temp = new JObject();
                                temp.Add("num", num);
                                temp.Add("pokerType", pokerType);
                                jarray.Add(temp);
                            }
                        }
                        backData.Add("pokerList", jarray);

                        if (listPoker.Count > 0)
                        {
                            backData.Add("hasOutPoker", true);
                        }
                        else
                        {
                            backData.Add("hasOutPoker", false);
                        }
                    }

                    //LogUtil.getInstance().addDebugLog(m_logFlag + "----" + "托管出牌：" + playerData.m_uid + "  " + backData.ToString());
                    DDZ_GameLogic.doTask_ReceivePlayerOutPoker(gameBase, playerData.m_connId, backData.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ":trusteeshipLogic_OutPoker：" + ex);
        }
    }
    
    // 托管:抢地主
    public static void trusteeshipLogic_QiangDiZhu(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            LogUtil.getInstance().writeRoomLog(room, ":托管：帮" + playerData.m_uid + "抢地主");

            JObject data = new JObject();

            data["tag"] = room.m_tag;
            data["uid"] = playerData.m_uid;
            data["playAction"] = (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_QiangDiZhu;

            // 机器人叫分
            if (playerData.m_isAI)
            {
                if (room.m_maxJiaoFenPlayerData == null)
                {
                    int r = RandomUtil.getRandom(1, 3);
                    data["fen"] = r;
                }
                else
                {
                    int r = RandomUtil.getRandom(room.m_maxJiaoFenPlayerData.m_jiaofen + 1, 3);
                    data["fen"] = r;
                }
            }
            // 真人托管不叫分
            else
            {
                data["fen"] = 0;
            }

            DDZ_GameLogic.doTask_QiangDiZhu(gameBase, playerData.m_connId, data.ToString());
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".trusteeshipLogic_QiangDiZhu: " + ex);
        }
    }

    // 托管:加棒
    public static void trusteeshipLogic_JiaBang(DDZ_GameBase gameBase, DDZ_RoomData room, DDZ_PlayerData playerData)
    {
        try
        {
            LogUtil.getInstance().writeRoomLog(room, ":托管：帮" + playerData.m_uid + "加棒");

            JObject data = new JObject();

            data["tag"] = room.m_tag;
            data["uid"] = playerData.m_uid;
            data["playAction"] = (int)TLJCommon.Consts.DDZ_PlayAction.PlayAction_JiaBang;
            data["isJiaBang"] = 1;

            DDZ_GameLogic.doTask_JiaBang(gameBase, playerData.m_connId, data.ToString());
        }
        catch (Exception ex)
        {
            TLJ_PlayService.PlayService.log.Error(m_logFlag + "----" + ".trusteeshipLogic_JiaBang: " + ex);
        }
    }
}
