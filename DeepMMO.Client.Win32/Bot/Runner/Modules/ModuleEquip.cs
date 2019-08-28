using System.Collections.Generic;
using pomelo.item;
using ZeusBattleClientBot;
using pomelo.area;
using pomelo.connector;
using CommonLang.Reflection;

namespace ZeusBotTest.Runner
{
    public class ModuleEquip : BotRunner.RunnerModule
    {
        List<Grid> Equips;
        List<Grid> Bags;
        int pro;
        int upLevel;
        Dictionary<int, string> proTable;
        Dictionary<string, ItemDetail> ItemDetails;
        Dictionary<string, int> equipPosition;

        bool isFirstBind = true;

        public ModuleEquip(BotRunner r) : base(r)
        {
            proTable = new Dictionary<int, string>();
            ItemDetails = new Dictionary<string, ItemDetail>();
            equipPosition = new Dictionary<string, int>();

            this.proTable[1] = "狂战士";
            this.proTable[2] = "刺客";
            this.proTable[3] = "魔法师";
            this.proTable[4] = "猎人";
            this.proTable[5] = "牧师";

            this.equipPosition["主手"] = 1;
            this.equipPosition["副手"] = 2;
            this.equipPosition["头部"] = 3;
            this.equipPosition["上衣"] = 4;
            this.equipPosition["腿部"] = 5;
            this.equipPosition["腰部"] = 6;
            this.equipPosition["手套"] = 7;
            this.equipPosition["鞋子"] = 8;
            this.equipPosition["勋章"] = 9;
            this.equipPosition["项链"] = 10;
            this.equipPosition["戒指"] = 11;
            this.equipPosition["护身符"] = 12;

            client.GameSocket.listen<BagItemUpdatePush>(on_item_update);
            client.GameSocket.listen<EquipmentSimplePush>(on_equip_update);
            client.GameSocket.listen<ItemDetailPush>(on_item_detail_update);
        }
        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
            Equips = e.s2c_player.equipments.equips;
            Bags = e.s2c_player.store.bag.bagGrids;
            pro = e.s2c_player.pro;
            upLevel = 0;
            try_get_all_equip_detail();
            if (isFirstBind)
            {
                runner.do_gm_add_item();
                runner.do_gm_add_gold(99999999);
                isFirstBind = false;
            }
        }
        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimeDelayMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    
                    
                }
            });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    try_equip();
                    //try_un_equip();
                    try_equip_melt();

                }
            });
        }
        private void try_get_all_equip_detail()
        {
            client.GameSocket.itemHandler.getAllEquipDetailsRequest(
                    (err, rsp) =>
                    {
                        foreach (var item in rsp.s2c_items)
                        {
                            ItemDetails[item.id] = item;
                        }
                    });
        }
        private void try_un_equip()
        {
            int randValue = bot.Random.Next(0, Equips.Count - 1);

            client.GameSocket.equipHandler.unEquipRequest(
                Equips[randValue].gridIndex, (err, rsp) =>
                { });
        }
        private void try_equip()
        {
            foreach (var bag in Bags)
            {
                if (bag.item.itemType < 1 || bag.item.itemType > 4) continue;

                object qc;
                int qid;
                Dictionary<string, object> template = BotClientManager.GetItemTemplate(bag.item.code);

                if (!template.TryGetValue("Pro", out qc) || proTable[pro] != qc.ToString()) continue;

                if (!template.TryGetValue("UpReq", out qc) || !int.TryParse(qc.ToString(), out qid) || (qid > 0 && qid > upLevel)) continue;

                ItemDetail ItemDetail = ItemDetails[bag.item.id];

                if (null == ItemDetail) continue;

                if (null == ItemDetail.equipDetail) continue;

                if (0 == ItemDetail.equipDetail.isIdentfied) continue;

                if (!template.TryGetValue("Type", out qc)) continue;

                //武器不用换
                if (equipPosition[qc.ToString()] == 1) continue;
              

                Item equip = get_equip_by_pos(equipPosition[qc.ToString()]);

              

                if(null != equip)
                {
                    ItemDetail equipDetail = ItemDetails[equip.id];

                    if(null != equipDetail && ItemDetail.equipDetail.score < equipDetail.equipDetail.score)
                    {
                        continue;
                    }

                   
                }

                client.GameSocket.equipHandler.equipRequest(
            bag.gridIndex, (err, rsp) =>
            { });
            }
        }
        private void on_item_update(BagItemUpdatePush e)
        {
            foreach (var i in e.s2c_data)
            {
                foreach (var bag in Bags)
                {
                    if (i.gridIndex == bag.gridIndex)
                    {
                        bag.item = i.item;
                        break;
                    }
                }
            }
        }
        private void on_equip_update(EquipmentSimplePush e)
        {
            foreach (var i in e.s2c_data)
            {
                foreach (var equip in Equips)
                {
                    if (i.gridIndex == equip.gridIndex)
                    {
                        equip.item = i.item;
                        break;
                    }
                }
            }
        }
        private void on_item_detail_update(ItemDetailPush e)
        {
            foreach (var i in e.s2c_data)
            {
                ItemDetails[i.id] = i;
            }
        }
        private Item get_equip_by_pos(int pos)
        {
            foreach (var equip in Equips)
            {
                if(equip.gridIndex == pos)
                {
                    return equip.item;
                }
            }
            return null;
        }
        private void try_equip_melt()
        {
            List<int> indexs = new List<int>();

            foreach (var bag in Bags)
            {
                if (bag.item.itemType < 1 || bag.item.itemType > 4) continue;

                object qc;
                int qid;
                Dictionary<string, object> template = BotClientManager.GetItemTemplate(bag.item.code);

                if (!template.TryGetValue("NoMelt", out qc) || !int.TryParse(qc.ToString(), out qid) || (qid == 1)) continue;

                indexs.Add(bag.gridIndex);
            }

            client.GameSocket.equipHandler.equipMeltRequest(
                indexs, (err, rsp) =>
                { });
        }
        [Desc("装备配置")]
        [Expandable]
        public class Config
        {
            [Desc("装备检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "装备配置";
            }
        }
    }
}
