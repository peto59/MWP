namespace MWP.UIBinding;

public interface IView
{
    public void RunOnUiThread(Action action);
}