namespace WholesomeTBCAIO.Rotations
{
    public interface IClassRotation
    {
        public void Initialize(IClassRotation specialization);
        public void Dispose();
    }
}
