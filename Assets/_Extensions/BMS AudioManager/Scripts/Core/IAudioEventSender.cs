public interface IAudioEventSender
{
    //TODO Add getters and setters for the  properties - currently not needed - future updates will require them
    // string EventName { get; set; }
    // bool PlayOnEnabled { get; set; }
    // float EventDelay { get; set; }
    //  more to come...
    // note: we are implementing this interface to ensure that all audio event senders have a consistent structure - technically we dont need them

    void Play();
    void Stop();
    void Pause();
}