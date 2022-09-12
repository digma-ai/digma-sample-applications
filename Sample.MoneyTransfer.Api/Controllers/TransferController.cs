﻿using System.Diagnostics;
using Automatonymous;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.MoneyTransfer.API.Consumer;
using Sample.MoneyTransfer.API.Data;
using Sample.MoneyTransfer.API.Domain.Services;
using Sample.MoneyTransfer.API.DTO;
using Sample.MoneyTransfer.API.Utils;
using Sample.ReportTracking;

namespace Sample.MoneyTransfer.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class TransferController : ControllerBase
{
    private static readonly ActivitySource Activity = new(nameof(TransferController));
    private readonly Gringotts  moneyVault;
    private readonly ILogger<TransferController> _logger;
    private readonly IMoneyTransferDomainService moneyTransferDomainService;
    private readonly IMessagePublisher _messagePublisher;

    public TransferController(Gringotts  moneyVault,
                              ILogger<TransferController> logger,
                              IMoneyTransferDomainService moneyTransferDomainService,
                              IMessagePublisher messagePublisher
                              )
    {
        this.moneyVault = moneyVault;
        _logger = logger;
        this.moneyTransferDomainService = moneyTransferDomainService;
        _messagePublisher = messagePublisher;
    }

    [HttpPost(Name = "deposit")]
    public async Task DepositFunds(DepositRequest request)
    {
        using var activity = Activity.StartActivity();

        var account = await moneyVault.Accounts.FindAsync(request.AccountId);
        await moneyTransferDomainService.DepositeFunds(account.Id, request.Amount);

        await _messagePublisher.Publish(new ReportRequestReceivedEvent(
            new ReportSagaState()
            {
                CustomerId = "customer-1234",
                ReportId = "test",
                RequestTime = DateTime.Now

            })
        );


    }

    [HttpPost(Name = "transfer")]
    public async Task<TransferResult> TransferFunds(TransferRequest request)
    {
        using (var activity = Activity.StartActivity("Process transfer", ActivityKind.Internal)){
            var transferRecord = await moneyTransferDomainService.TransferFunds(request.SouceAccountId,
                                                     request.TargetAccountId,
                                                     request.Amount);
            await _messagePublisher.Publish(new TransferFundsEvent
            {
                TransferRecord = transferRecord,
                DelayInMS = 3000
            });
            return new TransferResult { Success = true, TransferDate = transferRecord.TransferTime };
        }
    }
}

