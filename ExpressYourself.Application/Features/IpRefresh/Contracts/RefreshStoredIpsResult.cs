namespace ExpressYourself.Application.Features.IpRefresh.Contracts
{
    public sealed record RefreshStoredIpsResult(int Scanned, int Changed, int Unchanged, int Failed);
}