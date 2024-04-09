namespace EventBus.Base;

public class  SubscribtionInfo
{
    public Type HandlerType { get;}
    public SubscribtionInfo(Type handlerType)
    {
        HandlerType = handlerType ?? throw new ArgumentException(nameof(handlerType));
    }
    public static SubscribtionInfo Typed(Type handlerType)
    {   
        return new SubscribtionInfo(handlerType);
    }

}