namespace RuniOS.Inspectors.Attributes
{
    public interface IInspectorAttribute
    {
        int priority => 0;
        
        bool applyToSelf => false;
        bool inheritToChildren => false;
    }
}