﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLJ_PlayService;

class Request_UserInfo_Game
{
    public static void doRequest(string uid)
    {
        try
        {
            JObject respondJO = new JObject();

            respondJO.Add("tag", TLJCommon.Consts.Tag_UserInfo_Game);
            respondJO.Add("uid", uid);
            respondJO.Add("isClientReq", 0);

            // 传给数据库服务器
            {
                if (!PlayService.m_mySqlServerUtil.sendMseeage(respondJO.ToString()))
                {
                    // 连接不上数据库服务器
                }
            }
        }
        catch (Exception ex)
        {
            // 客户端参数错误
            LogUtil.getInstance().addErrorLog("Request_UserInfo_Game.doRequest----" + ex.Message);
        }
    }

    public static void onMySqlRespond(string respondData)
    {
        try
        {
            JObject jo = JObject.Parse(respondData);
            string uid = jo.GetValue("uid").ToString();

            PlayerData playerDate = GameUtil.getRoomByUid(uid).getPlayerDataByUid(uid);
            playerDate.m_buffData.Clear();

            JArray buff_list = (JArray)JsonConvert.DeserializeObject(jo.GetValue("BuffData").ToString());

            for (int i = 0; i < buff_list.Count; i++)
            {
                int prop_id = (int)buff_list[i]["PropId"];
                int buff_num = (int)buff_list[i]["PropNum"];
                playerDate.m_buffData.Add(new BuffData(prop_id, buff_num));
            }
        }
        catch (Exception ex)
        {
            LogUtil.getInstance().addErrorLog("Request_UserInfo_Game.onMySqlRespond----" + ex.Message);

            // 客户端参数错误
            //respondJO.Add("code", Convert.ToInt32(TLJCommon.Consts.Code.Code_ParamError));

            // 发送给客户端
            //LogicService.m_serverUtil.sendMessage(connId, respondJO.ToString());
        }
    }
}