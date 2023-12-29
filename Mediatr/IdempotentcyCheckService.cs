namespace KafkaMT.Mediatr;

public class IdempotentcyCheckService
{
	public async Task<bool> IsProcessed<T>(T request, Guid idempotencyKey)
		where T : class, IHasIdempotencyKey
	{
		// async db call here

		if (typeof(T).FullName == "KafkaMT.Mediatr.Request" && idempotencyKey == Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e"))
		{
			return true;
		}

		return false;
	}
}
