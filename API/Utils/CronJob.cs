﻿using System;
using System.Threading;
using System.Threading.Tasks;
using API.Models.Context;
using API.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NET_base.Models.Common;

public class CronJob : BackgroundService
{
    private readonly ILogger<CronJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private Timer _timer;
    private readonly int INTERVAL_SECOND = 15;

    public CronJob(ILogger<CronJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(INTERVAL_SECOND));
        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        _logger.LogInformation("Running scheduled task at: {time}", DateTimeOffset.Now);

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DBContext>();

                var items = await _context.Items
                    .Where(item => item.BidStatus != Constant.ENDED && item.BidEndDate <= DateTime.UtcNow)
                    .Include(item => item.Seller)
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.BidStatus = Constant.ENDED;
                    item.UpdatedAt = DateTime.UtcNow;

                    var highestBid = await _context.Bids
                        .Where(bid => bid.ItemId == item.Id && bid.BidAmount.HasValue)
                        .Include(bid => bid.Bidder)
                        .OrderByDescending(bid => bid.BidAmount)
                        .FirstOrDefaultAsync();

                    if (highestBid != null)
                    {
                        var bidderEmailContent = $@"
    Hello {highestBid.Bidder.Username},

    Congratulations! You have won the auction for the item titled '{item.Title}'.

    Here are the details of the item:
    - Title: {item.Title}
    - Description: {item.Description}
    - Final Bid Amount: {highestBid.BidAmount:C}

    Please contact the seller to arrange payment and delivery:
    - Seller Name: {item.Seller.Username}
    - Seller Email: {item.Seller.Email}

    Thank you for participating in the auction!

    Regards,
    Auction Team
";

                        await EmailService.SendMailAsync(highestBid.Bidder.Email, "Auction Won - Congratulations!", bidderEmailContent);

                        var sellerEmailContent = $@"
    Hello {item.Seller.Username},

    Your auction for the item titled '{item.Title}' has ended successfully.

    Here are the details:
    - Title: {item.Title}
    - Description: {item.Description}
    - Winning Bid Amount: {highestBid.BidAmount:C}
    - Winner: {highestBid.Bidder.Username}
    - Winner Email: {highestBid.Bidder.Email}

    Please contact the winner to finalize the payment and delivery process.

    Thank you for using our auction platform!

    Regards,
    Auction Team
";

                        await EmailService.SendMailAsync(item.Seller.Email, "Auction Ended - Contact Winner", sellerEmailContent);
                    }

                    _context.Update(item);
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Job error: {Message}", e.Message);
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}