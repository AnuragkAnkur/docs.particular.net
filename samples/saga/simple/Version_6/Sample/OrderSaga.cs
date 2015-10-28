﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

#region thesaga
public class OrderSaga : Saga<OrderSagaData>,
    IAmStartedByMessages<StartOrder>,
    IHandleMessages<CompleteOrder>,
    IHandleTimeouts<CancelOrder>
{
    static ILog logger = LogManager.GetLogger(typeof(OrderSaga));

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
    {
        mapper.ConfigureMapping<StartOrder>(m => m.OrderId)
                .ToSaga(s => s.OrderId);
        mapper.ConfigureMapping<CompleteOrder>(m => m.OrderId)
                .ToSaga(s => s.OrderId);
    }

    public async Task Handle(StartOrder message, IMessageHandlerContext context)
    {
        Data.OrderId = message.OrderId;
        logger.Info(string.Format("Saga with OrderId {0} received StartOrder with OrderId {1}", Data.OrderId, message.OrderId));
        await context.SendLocalAsync(new CompleteOrder
        {
            OrderId = Data.OrderId
        });
        await RequestTimeoutAsync<CancelOrder>(context, TimeSpan.FromMinutes(30));
    }

    public Task Handle(CompleteOrder message, IMessageHandlerContext context)
    {
        logger.Info(string.Format("Saga with OrderId {0} received CompleteOrder with OrderId {1}", Data.OrderId, message.OrderId));
        MarkAsComplete();
        return Task.FromResult(0);
    }

    public Task Timeout(CancelOrder state, IMessageHandlerContext context)
    {
        logger.Info(string.Format("Complete not received soon enough OrderId {0}", Data.OrderId));
        MarkAsComplete();
        return Task.FromResult(0);
    }
}
#endregion