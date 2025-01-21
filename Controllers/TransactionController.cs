using BosnetTest.Model.dto;
using BosnetTest.Service;
using Microsoft.AspNetCore.Mvc;

namespace BosnetTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            return _transactionService.SetorAtauTarik(request, "Setor");
        }

        [HttpPut("tarik")]
        public TransactionResponse Tarik([FromBody] TransactionRequest request)
        {
            return _transactionService.SetorAtauTarik(request, "Tarik");
        }

        [HttpPut("transfer")]
        public TransactionResponse Transfer([FromBody] TransactionTransferRequest request)
        {
            TransactionResponse transactionResponse = new TransactionResponse();
            try
            {
                //tranfer masuk
                TransactionRequest transactionRequestTransfer1 = new TransactionRequest();
                transactionRequestTransfer1.amount = request.amount;
                transactionRequestTransfer1.account = request.accountTo;
                transactionRequestTransfer1.currency = request.currency;
                _transactionService.SetorAtauTarik(transactionRequestTransfer1, "Transfer Setor");

                //tranfer keluar
                TransactionRequest transactionRequestTransfer2 = new TransactionRequest();
                transactionRequestTransfer2.amount = request.amount;
                transactionRequestTransfer2.account = request.accountFrom;
                transactionRequestTransfer2.currency = request.currency;
                _transactionService.SetorAtauTarik(transactionRequestTransfer1, "Transfer Tarik");

                transactionResponse.amount = request.amount;
                transactionResponse.currency = request.currency;
                transactionResponse.type = "Transfer";
                transactionResponse.status = "Sukses";
                transactionResponse.message = "Transfer berhasil dilakukan";
                return transactionResponse;
            }
            catch (Exception ex)
            {
                transactionResponse.amount = request.amount;
                transactionResponse.currency = request.currency;
                transactionResponse.type = "Transfer";
                transactionResponse.status = "Failed";
                transactionResponse.message = "Transfer gagal dilakukan";
                return transactionResponse;
            }

        }
    }
}

    
