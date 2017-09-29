﻿using System;
using System.Collections.Generic;
using System.Linq;
using TLJCommon;


public class PlayRuleUtil
{
    /// <summary>
    /// 只能判断是否是一个连续的拖拉机
    /// </summary>
    /// <param name="playerOutPokerList"></param>
    /// <param name="mLevelPokerNum"></param>g
    /// <param name="masterPokerType"></param>
    /// <returns></returns>
    public static bool IsTuoLaJi(List<PokerInfo> playerOutPokerList, int mLevelPokerNum, int masterPokerType)
    {
        if (playerOutPokerList.Count % 2 == 0 && playerOutPokerList.Count >= 4)
        {
            //都是主牌或者都是同一花色的副牌
            if (IsAllMasterPoker(playerOutPokerList, mLevelPokerNum, masterPokerType) ||
                IsAllFuPoker(playerOutPokerList))
            {
                //先判断是否为对子
                for (int i = 0; i < playerOutPokerList.Count; i += 2)
                {
                    if (playerOutPokerList[i].m_num != playerOutPokerList[i + 1].m_num
                        || playerOutPokerList[i].m_pokerType != playerOutPokerList[i + 1].m_pokerType)
                    {
                        return false;
                    }
                }

                //判断权重
                for (int i = 0; i < playerOutPokerList.Count - 2; i += 2)
                {
                    if (Math.Abs(playerOutPokerList[i + 2].m_weight - playerOutPokerList[i].m_weight) != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    //单牌是否为主牌
    public static bool IsMasterPoker(PokerInfo pokerInfo, int mLevelPokerNum, int masterPokerType)
    {
        if (pokerInfo.m_num == mLevelPokerNum || pokerInfo.m_pokerType == (Consts.PokerType) masterPokerType
            || pokerInfo.m_pokerType == Consts.PokerType.PokerType_Wang)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //是否都是主牌
    public static bool IsAllMasterPoker(List<PokerInfo> list, int mLevelPokerNum, int masterPokerType)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (!IsMasterPoker(list[i], mLevelPokerNum, masterPokerType))
            {
                return false;
            }
        }
        return true;
    }

    //是否都是同一花色
    public static bool IsAllFuPoker(List<PokerInfo> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (list[i].m_pokerType != list[i + 1].m_pokerType)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    ///  给weight重新赋值，从2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17
    ///  17为大王，16为小王，15为主级牌,14为副级牌
    ///  无主情况下,16为大王,15为小王，14为级牌
    /// </summary>
    /// <param name="list">牌</param>
    /// <param name="levelPokerNum">级牌</param>
    /// <param name="masterPokerType">主牌花色</param>
    /// <returns></returns>
    public static List<PokerInfo> SetPokerWeight(List<PokerInfo> list, int levelPokerNum,
        Consts.PokerType masterPokerType)
    {
        for (int i = 0; i < list.Count; i++)
        {
            PokerInfo pokerInfo = list[i];
            //是级牌
            if (pokerInfo.m_num == levelPokerNum)
            {
                if (pokerInfo.m_pokerType == masterPokerType)
                {
                    pokerInfo.m_weight = 15;
                }
                else
                {
                    pokerInfo.m_weight = 14;
                }
            }
            //大王
            else if (pokerInfo.m_num == 16)
            {
                pokerInfo.m_weight = (int) masterPokerType != (-1) ? 17 : 16;
            }
            //小王
            else if (pokerInfo.m_num == 15)
            {
                pokerInfo.m_weight = (int) masterPokerType != (-1) ? 16 : 15;
            }
            else if (pokerInfo.m_num < levelPokerNum)
            {
                pokerInfo.m_weight = pokerInfo.m_num;
            }
            else
            {
                pokerInfo.m_weight = pokerInfo.m_num - 1;
            }
        }
        return list;
    }

    /// <summary>
    ///  给weight重新赋值，从2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17
    ///  17为大王，16为小王，15为主级牌,14为副级牌
    ///  无主情况下,16为大王,15为小王，14为级牌
    /// </summary>
    /// <param name="room">房间数据</param>
    /// <returns></returns>
    public static void SetAllPokerWeight(RoomData room)
    {
        List<PlayerData> playerDatas = room.getPlayerDataList();
        for (int i = 0; i < playerDatas.Count; i++)
        {
            PlayerData playerData = playerDatas[i];
            List<PokerInfo> pokerInfos = playerData.getPokerList();
            SetPokerWeight(pokerInfos, room.m_levelPokerNum, (Consts.PokerType) room.m_masterPokerType);
        }
    }

    /// <summary>
    /// 得到甩牌是否成功后的牌
    /// </summary>
    /// <param name="room"></param>
    /// <param name="outPokerList"></param>
    /// <returns></returns>
    public static List<PokerInfo> GetShuaiPaiPoker(RoomData room, List<PokerInfo> outPokerList)
    {
        List<PokerInfo> resultList = new List<PokerInfo>();
        //设置牌的权重
        SetPokerWeight(outPokerList, room.m_levelPokerNum, (Consts.PokerType) room.m_masterPokerType);

        List<PlayerData> playerDatas = room.getPlayerDataList();

        //得到甩牌的对子
        List<PokerInfo> firestDoubleList = GetDoublePoker(outPokerList);
        //得到甩牌的单牌
        List<PokerInfo> firestSingleList = GetSinglePoker(outPokerList, firestDoubleList);

        //甩牌中最小的单牌

        PokerInfo minSingle = null;
        if (firestSingleList.Count > 0)
        {
            minSingle = firestSingleList[0];
        }
        //如果甩的牌都是主牌
        if (IsAllMasterPoker(outPokerList, room.m_levelPokerNum, room.m_masterPokerType))
        {
            for (int i = 1; i < playerDatas.Count; i++)
            {
                //得到其余玩家的手牌
                List<PokerInfo> pokerList = playerDatas[i].getPokerList();
                List<PokerInfo> masterPoker = GetMasterPoker(pokerList, room.m_levelPokerNum, room.m_masterPokerType);
                if (masterPoker.Count > 0)
                {
                    //得到主牌中的对子和单牌
                    List<PokerInfo> OtherDoubleleList = GetDoublePoker(masterPoker);
                    List<PokerInfo> OtherSingleList = GetSinglePoker(masterPoker, OtherDoubleleList);
                    //没有单牌
                    if (firestSingleList.Count == 0)
                    {
                        return CompareDoublePoker(firestDoubleList, OtherDoubleleList,room.m_levelPokerNum,room.m_masterPokerType);
                    }
                    //最小的单牌牌都比其他玩家手牌中的最大主牌大else
                    else if (minSingle.m_weight >= masterPoker[masterPoker.Count - 1].m_weight)
                    {
                        return CompareDoublePoker(firestDoubleList, OtherDoubleleList,room.m_levelPokerNum,room.m_masterPokerType);
                    }
                    //甩牌失败,单牌比别人小
                    else
                    {
                        resultList.Add(minSingle);
                        return resultList;
                    }
                }
                //其他玩家没有主牌
                else
                {
                    //该玩家牌大，不作处理
                }
            }
        }
        //同花色的副牌
        else if (IsAllFuPoker(outPokerList))
        {
            Consts.PokerType mPokerType = outPokerList[0].m_pokerType;
            for (int i = 1; i < playerDatas.Count; i++)
            {
                //得到其余玩家的手牌
                List<PokerInfo> pokerList = playerDatas[i].getPokerList();
                //得到指定花色的牌
                List<PokerInfo> pokerByType = GetPokerByType(pokerList, mPokerType);

                if (pokerByType.Count > 0)
                {
                    //得到副牌中的对子和单牌
                    List<PokerInfo> OtherDoubleleList = GetDoublePoker(pokerByType);
                    List<PokerInfo> OtherSingleList = GetSinglePoker(pokerByType, OtherDoubleleList);
                    //没有单牌
                    if (firestSingleList.Count == 0)
                    {
                        return CompareDoublePoker(firestDoubleList, OtherDoubleleList, room.m_levelPokerNum,
                            room.m_masterPokerType);
                    }
                    //最小的单牌牌都比其他玩家手牌中的最大主牌大else
                    else if (minSingle.m_weight >= pokerByType[pokerByType.Count - 1].m_weight)
                    {
                        return CompareDoublePoker(firestDoubleList, OtherDoubleleList, room.m_levelPokerNum,
                            room.m_masterPokerType);
                    }
                    //甩牌失败,单牌比别人小
                    else
                    {
                        resultList.Add(minSingle);
                        return resultList;
                    }
                }
                //其他玩家没有副牌
                else
                {
                    //该玩家牌大，不作处理
                }

            }
        }
        return resultList;
    }


    //比较甩牌中的对子
    private static List<PokerInfo> CompareDoublePoker(List<PokerInfo> firestDoubleList, List<PokerInfo> OtherDoubleleList,
        int mLevelPokerNum,int mMasterPokerType)
    {
        List<PokerInfo> list = new List<PokerInfo>();
        //继续比较对子
        if (firestDoubleList.Count > 0)
        {
            if (OtherDoubleleList.Count > 0)
            {
                List<List<PokerInfo>> firstAllTlj =
                    GetAllTljFromDouble(firestDoubleList, mLevelPokerNum, mMasterPokerType);
                List<List<PokerInfo>> OtherAllTlj =
                    GetAllTljFromDouble(OtherDoubleleList, mLevelPokerNum, mMasterPokerType);
                //如果甩牌的对子中包括拖拉机,则和其他玩家手中的拖拉机比较
                if (firstAllTlj.Count > 0)
                {
                    if (OtherAllTlj.Count > 0)
                    {
                        for (int j = 0; j < firstAllTlj.Count; j++)
                        {
                            List<PokerInfo> TljPoker = firstAllTlj[j];
                            for (int k = 0; k < OtherAllTlj.Count; k++)
                            {
                                var otherTljPoker = OtherAllTlj[k];
                                //第一个拖拉机的对子多于其他玩家手中的拖拉机
                                if (TljPoker.Count > otherTljPoker.Count)
                                {
                                }
                                else
                                {
                                    //第一个拖拉机最小对子大于其他玩家手中的拖拉机最大的
                                    if (TljPoker[0].m_weight >=
                                        otherTljPoker[otherTljPoker.Count - 1].m_weight)
                                    {
                                    }
                                    else
                                    {
                                        {
                                            list = TljPoker;
                                            return list;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //其他玩家手中没有拖拉机
                    else
                    {
                        //该玩家牌大，不作处理 
                    }
                }
                //甩牌为多个不连续对子
                else
                {
                    if (OtherAllTlj.Count > 0)
                    {
                        List<PokerInfo> pokerInfos = OtherAllTlj[OtherAllTlj.Count - 1];
                        if (firestDoubleList[0].m_weight >= pokerInfos[pokerInfos.Count - 1].m_weight)
                        {
                            //该玩家牌大，不作处理
                        }
                        else
                        {
                            //甩牌失败,单牌大，但是对子比别人小
                            list.Add(firestDoubleList[0]);
                            list.Add(firestDoubleList[1]);
                            return list;
                        }
                    }
                    else
                    {
                        //最小的牌都比其他玩家手牌中的最大主牌大
                        if (firestDoubleList[0].m_weight >= OtherDoubleleList[OtherDoubleleList.Count - 1].m_weight)
                        {
                            //该玩家牌大，不作处理
                        }
                        //甩牌失败,单牌大，但是对子比别人小
                        else
                        {
                            list.Add(firestDoubleList[0]);
                            list.Add(firestDoubleList[1]);
                            return list;
                        }
                    }
                  
                }
            }
            //其他玩家没有对子，甩牌成功
            else
            {
                //该玩家牌大，不作处理 
            }
        }
        return list;
    }

    /// <summary>
    /// 得到指定花色的牌,并排序
    /// </summary>
    /// <param name="pokerList"></param>
    /// <param name="mPokerType"></param>
    private static List<PokerInfo> GetPokerByType(List<PokerInfo> pokerList, Consts.PokerType mPokerType)
    {
        List <PokerInfo> list = new List<PokerInfo>();
        for (int i = 0; i < pokerList.Count; i++)
        {
            var poker = pokerList[i];
            if (poker.m_pokerType == mPokerType)
            {
                list.Add(poker);
            }
        }
        return list.OrderBy(a => a.m_weight).ToList();

    }

    /// <summary>
    /// 通过去除对子来得到单牌,并通过权重排序
    /// </summary>
    /// <param name="outPokerList"></param>
    /// <param name="firestDoubleList"></param>
    /// <returns></returns>
    public static List<PokerInfo> GetSinglePoker(List<PokerInfo> PokerList, List<PokerInfo> DoubleList)
    {
        List<PokerInfo> firestSingleList  = new List<PokerInfo>();
        firestSingleList = firestSingleList.Union(PokerList.Except(DoubleList).ToList()).ToList();
        return firestSingleList.OrderBy(a => a.m_weight).ToList();
    }

    /// <summary>
    /// 得到牌中对子，并通过权重排序
    /// 如果是3副以上的牌，此算法有问题
    /// </summary>
    /// <param name="PokerList"></param>
    /// <returns></returns>
    public static List<PokerInfo> GetDoublePoker(List<PokerInfo> PokerList)
    {
        List<PokerInfo> firestDoubleList = new List<PokerInfo>();
        for (int i = 0; i < PokerList.Count - 1; i++)
        {
            PokerInfo pokerInfo = PokerList[i];
            for (int j = i + 1; j < PokerList.Count; j++)
            {
                if (pokerInfo.m_num == PokerList[j].m_num && pokerInfo.m_pokerType == PokerList[j].m_pokerType)
                {
                    firestDoubleList.Add(PokerList[i]);
                    firestDoubleList.Add(PokerList[j]);
                }
            }
        }
        return firestDoubleList.OrderBy(a => a.m_weight).ToList();
    }

    /// <summary>
    /// 从对子中得到所有的拖拉机
    /// </summary>
    /// <param name="doubleList"></param>
    /// <param name="mLevelPokerNum"></param>
    /// <param name="masterPokerType"></param>
    /// <returns></returns>
    public static List<List<PokerInfo>> GetAllTljFromDouble(List<PokerInfo> doubleList, int mLevelPokerNum, int masterPokerType)
    {
        List<List<PokerInfo>> list = new List<List<PokerInfo>>();
        while (doubleList.Count >= 4)
        {
            List<PokerInfo> tuoLaJi = GetTuoLaJi(doubleList,mLevelPokerNum,masterPokerType);
            if (tuoLaJi.Count == 0) break;
            foreach (var VARIABLEs in tuoLaJi)
            {
                doubleList.Remove(VARIABLEs);
            }
            list.Add(tuoLaJi);
        }
        return list;
    }

    /// <summary>
    /// 得到拖拉机
    /// </summary>
    /// <param name="playerOutPokerList"></param>
    /// <param name="mLevelPokerNum"></param>
    /// <param name="masterPokerType"></param>
    /// <returns></returns>
    public static List<PokerInfo> GetTuoLaJi(List<PokerInfo> playerOutPokerList, int mLevelPokerNum, int masterPokerType)
    {
        List<PokerInfo> pokerInfos = new List<PokerInfo>();
        if (playerOutPokerList.Count % 2 == 0 && playerOutPokerList.Count >= 4)
        {
            //都是主牌或者都是同一花色的副牌
            if (IsAllMasterPoker(playerOutPokerList, mLevelPokerNum, masterPokerType) ||
                IsAllFuPoker(playerOutPokerList))
            {
                int temp = 1;
                //判断权重
                for (int i = 0; i < playerOutPokerList.Count - 2; i += 2)
                {
                    if (Math.Abs(playerOutPokerList[i + 2].m_weight - playerOutPokerList[0].m_weight) == temp)
                    {
                        pokerInfos.Add(playerOutPokerList[i + 2]);
                        pokerInfos.Add(playerOutPokerList[i + 3]);
                    }
                    temp++;
                }
                if (pokerInfos.Count > 0)
                {
                    pokerInfos.Add(playerOutPokerList[0]);
                    pokerInfos.Add(playerOutPokerList[1]);
                }
            }
        }
        return pokerInfos.OrderBy(a => a.m_weight).ToList();
    }

    /// <summary>
    /// 得到手牌中的所有主牌
    /// </summary>
    /// <param name="pokerInfos"></param>
    /// <param name="mLevelPokerNum"></param>
    /// <param name="masterPokerType"></param>
    /// <returns></returns>
    public static List<PokerInfo> GetMasterPoker(List<PokerInfo> pokerInfos, int mLevelPokerNum, int masterPokerType)
    {
        List<PokerInfo> pokers = new List<PokerInfo>();
        for (int i = 0; i < pokerInfos.Count; i++)
        {
            PokerInfo pokerInfo = pokerInfos[i];
            if (IsMasterPoker(pokerInfo, mLevelPokerNum, masterPokerType))
            {
                pokers.Add(pokerInfo);
            }
        }
        return pokers.OrderBy(a => a.m_weight).ToList(); 
    }
}