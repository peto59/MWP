namespace MWP.Player.DataTypes;

public class MPVBaseResult
{
    public string error = "Empty MPV result";
    public bool status => error == "success";
    public string RawJson = String.Empty;
    public int request_id = int.MinValue;
    public object data = null;
    public string event_name = String.Empty;
    
}
public class MPVPlayingResult : MPVBaseResult
{
    public bool data = false;
}

public class MPVDurationResult : MPVBaseResult
{
    public long data = 0;
}