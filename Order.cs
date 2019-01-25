namespace CommanderAi2 { 
    public abstract class Order<T> where T : AiInterface {
        public abstract void Process(T world_object);
    }

    // expandable on user enum definitions
    public enum DefaultOrderResults : int {
        NONE=0,
        SUCCESS=1,
        FAILURE=2,
    }
}