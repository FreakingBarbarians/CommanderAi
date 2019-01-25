namespace CommanderAi2 {
    public interface AiInterface {
        void SetActor(AiBrain brain);
        AiBrain GetActor();
        void ClearOrder();
        void SetOrder();
    }
}