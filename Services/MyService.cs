namespace KafkaMT.Services;

public interface IMyService 
{
	public Task<bool> CheckIdempotency();
}

public class MyService : IMyService
{
	public Task<bool> CheckIdempotency()
	{
		return Task.FromResult(true);
	}
}
