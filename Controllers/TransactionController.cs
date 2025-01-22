using BosnetTest.Model.dto;
using BosnetTest.Service;
using Microsoft.AspNetCore.Mvc;

namespace BosnetTest.Controllers
{
    [ApiController]
    [Route("api/transaction")]
    public class TransactionController : ControllerBase
    {
        private CounterService _counterSercice;
        private TransactionService _transactionService;

        public TransactionController(CounterService counterService, TransactionService transactionService)
        {
            _counterSercice = counterService;
            _transactionService = transactionService;
        }

        [HttpPut("setor")]
        public TransactionResponse Setor([FromBody] TransactionRequest request)
        {
            return _transactionService.SetorAtauTarik(request, "SETOR");
        }

        [HttpPut("tarik")]
        public TransactionResponse Tarik([FromBody] TransactionRequest request)
        {
            return _transactionService.SetorAtauTarik(request, "TARIK");
        }

        [HttpPut("transfer")]
        public TransactionResponse Transfer([FromBody] TransactionTransferRequest requestList)
        {
            return _transactionService.Transfer(requestList);

        }

        [HttpGet("history")]
        public TransactionHistoryResponse TransactionHistory([FromQuery] string? account, [FromQuery] string? dateFrom, [FromQuery] string? dateTo)
        {
            TransactionHistoryRequest request = new TransactionHistoryRequest();
            request.account = account;
            request.dateFrom = dateFrom;
            request.dateTo = dateTo;
            return _transactionService.GetTransactionHistory(request);

        }
    }
}

    
