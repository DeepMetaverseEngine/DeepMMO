using DeepMMO.Unity3D.Terrain;

namespace DeepMMO.Unity3D.Terrian
{
    public abstract class BoxColliderFactory
    {
        static BoxColliderFactory()
        {
            new SimpleBoxColliderFactory();
        }

        public static BoxColliderFactory Factory { get; private set; }

        protected BoxColliderFactory()
        {
            Factory = this;
        }

        public virtual CheckBoxTouchComponent CreateBoxTouchComponent(float stepIntercept)
        {
            return new CheckBoxTouchComponent(stepIntercept);
        }
    }
}
