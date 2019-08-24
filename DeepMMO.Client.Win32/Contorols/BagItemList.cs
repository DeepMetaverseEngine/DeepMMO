using System;
using System.Windows.Forms;
using DDogClient.Protocol.Modules;
using DDogClient.Protocol.Modules.Package;

namespace CommonRPG.Client.Win32.Contorols
{
    public class BagItemList : ListView, IPackageListener
    {
        public BagItemList()
        {

        }

        private CommonBag mBag;

        private void ResetSize(int s)
        {
            this.Items.Clear();
            for (var i = 0; i < s; i++)
            {
                this.Items.Add(new ListViewItem("EMPTY"));
            }
        }

        private void Init()
        {
            ResetSize(mBag.Size);
            foreach (var slot in mBag.AllSlots)
            {
                UpdateItem(slot.Index, slot.Item as ItemData);
            }
        }

        public void Init(CommonBag bag)
        {
            mBag = bag;
            bag.AddListener(this);
            Init();
        }

        public void UpdateItem(int pos, ItemData data)
        {
            if (pos >= Items.Count)
            {
                Init();
            }
            else
            {
                Items[pos].Text = data == null ? "EMPTY" : $"{data.TemplateID}-{data.Count}";
            }
        }


        public bool Match(IPackageItem item)
        {
            return true;
        }

        public void OnItemAdded(BasePackage basePackage, int index)
        {
            UpdateItem(index, basePackage.GetItemAt<ItemData>(index));
        }

        public void OnItemRemoved(BasePackage basePackage, int index, IPackageItem lastItem)
        {
            UpdateItem(index, null);
        }

        public void OnItemCountChanged(BasePackage basePackage, int index, uint from, uint to)
        {
            OnItemAdded(basePackage, index);
        }

        public void OnUpdateAction(BasePackage package, UpdateAction[] acts)
        {
            foreach (var act in acts)
            {
                switch (act.Type)
                {
                    case UpdateAction.ActionType.Init:
                        Init();
                        break;
                    case UpdateAction.ActionType.Add:
                        UpdateItem(act.Index, package.GetItemAt<ItemData>(act.Index));
                        break;
                    case UpdateAction.ActionType.Remove:
                        UpdateItem(act.Index, package.GetItemAt<ItemData>(act.Index));
                        break;
                    case UpdateAction.ActionType.UpdateCount:
                        UpdateItem(act.Index, package.GetItemAt<ItemData>(act.Index));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}