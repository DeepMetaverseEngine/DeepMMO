using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Protocol;

namespace DeepMMO.Unity3D
{
    /// <summary>
    /// decorate AbstractBattle
    /// </summary>
    public class BattleDecorator
    {
        private AbstractBattle mBattle;

        public BattleDecorator(AbstractBattle battle)
        {
            mBattle = battle;
            mBattle.Layer.LayerInit += Layer_LayerInit;
            mBattle.Layer.ObjectEnter += Layer_ObjectEnter;
            mBattle.Layer.ObjectLeave += Layer_ObjectLeave;
            mBattle.Layer.MessageReceived += Layer_MessageReceived;
            mBattle.Layer.ObjectMessageReceived += Layer_OnObjectMessageReceived;
            mBattle.Layer.OnChangeBGM += Layer_OnChangeBGM;
            mBattle.Layer.ActorAdded += Layer_ActorAdded;
        }

        private void Layer_OnChangeBGM(LayerZone layer, string filename)
        {
        }

        private void Layer_OnObjectMessageReceived(LayerZone layer, IMessage msg, LayerZoneObject obj)
        {
        }

        private void Layer_LayerInit(LayerZone layer)
        {
        }

        private void Layer_ActorAdded(LayerZone layer, LayerPlayer actor)
        {
        }

        private void Layer_MessageReceived(LayerZone layer, IMessage msg)
        {
        }

        private void Layer_ObjectLeave(LayerZone layer, LayerZoneObject obj)
        {
        }

        private void Layer_ObjectEnter(LayerZone layer, LayerZoneObject obj)
        {
        }
    }
}