using BosnetTest.Model.dto;
using System.Data.OleDb;
using System.Data;
using BosnetTest.Model;
using System.Diagnostics.Metrics;

namespace BosnetTest.Service
{
    public class TransactionService
    {
        private readonly string _connectionString;
        private CounterService _counterService;
        private BosHistoryService _historyService;
        private BosBalanceService _balanceService;

        public TransactionService(IConfiguration configuration, CounterService counterService, BosHistoryService historyService, BosBalanceService bosBalanceService)
        {
            _connectionString = configuration.GetConnectionString("BosnetDbConnection");
            _counterService = counterService;
            _historyService = historyService;   
            _balanceService = bosBalanceService;
        }

        public TransactionResponse SetorAtauTarik(TransactionRequest request, String type)
        {
            TransactionResponse response = new TransactionResponse();
            response.type = type;
            response.currency = request.currency;
            response.amount = request.amount;
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    try
                    {
                        if(request.amount < 0)
                        {
                            throw new Exception("amount tidak boleh kurang dari 0");
                        }
                        //check apakah data history terakhir punya tanggal, currency dan tipe yang sama
                        var counterHistory = false;
                        var counterReset = false;
                        var stringCheck = $"SELECT TOP 1 * FROM [BOS_History] order by szTransactionId desc";
                        using (var checkCommand = new OleDbCommand(stringCheck, connection, transaction))
                        using (var reader = checkCommand.ExecuteReader())
                        {
                            var row = new Dictionary<string, object>();
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                            }

                            var checkDate = DateTime.Parse(row["dtmTransaction"].ToString());
                            if (DateTime.Now.Date != checkDate.Date) counterReset = true;
                            else if(row["szAccountId"].Equals(request.account) && !row["szCurrencyId"].Equals(request.currency))
                            {
                                counterHistory = true;
                            }

                            BOS_History bosHist = new BOS_History();
                            bosHist.dtmTransaction = DateTime.Now;
                            bosHist.decAmount = type.Contains("SETOR") ? request.amount : -1 * request.amount;
                            bosHist.szNote = type.Contains("TRANSFER") ? "TRANSFER" : type;
                            bosHist.szAccountId = request.account;
                            bosHist.szCurrencyId = request.currency;
                            int lastNumber = 0;
                            if(counterHistory)
                            {
                                bosHist.szTransactionId = row["szTransactionId"].ToString();
                            }
                            else
                            {
                                lastNumber = _counterService.GetTransactionIdLastNumber(connection, transaction);
                                if (counterReset) lastNumber = 0; // reset ke 0 di hari berbeda
                                lastNumber++;
                                bosHist.szTransactionId = _counterService.CreateTransactionId(lastNumber);
                            }

                            // insert to bos history
                            var status = _historyService.InsertToBosHistory(bosHist, connection, transaction);
                            if (status.Equals("Success"))
                            {
                                BOS_Balance bosBal = new BOS_Balance();
                                bosBal.szAccountId = request.account;
                                bosBal.szCurrencyId = request.currency;
                                bosBal.decAmount = request.amount;
                                // update bos balance
                                var statusBalance = _balanceService.UpdateBosBalance(bosBal, type, connection, transaction);
                                if (statusBalance.Equals("Success"))
                                {
                                    response.Status = "Success";
                                    response.message = type.Equals("SETOR") ? "Setor Success" : "Tarik Success";
                                    if(!counterHistory) _counterService.GetAndUpdateTransactionIdLastNumber(lastNumber, connection, transaction);
                                }
                                else throw new Exception("Failed ketika Update Balance");
                            }
                            else throw new Exception("Failed ketika Update History");
                        }
                        transaction.Commit();
                        return response;

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.Status = "Failed, error : " + ex.Message;
                        if(type.Equals("SETOR")) response.message = "Setor Failed";
                        if (type.Equals("TARIK")) response.message = "Tarik Failed";
                        return response;
                    }
                }
            }
        }

        public TransactionResponse Transfer(TransactionTransferRequest requestList)
        {
            TransactionResponse response = new TransactionResponse();
            var message = "";
            response.type = "TRANSFER";
            foreach (var request in requestList.Transfers)
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                    {
                        try
                        {
                            var lastNumber = 0;
                            for (var i = 0; i < 2; i++)
                            {
                                // 0 = uang masuk, 1 = uang keluar
                                TransactionRequest transferData = new TransactionRequest();
                                transferData.amount = request.Amount;
                                transferData.account = i == 0 ? request.AccountTo : request.AccountFrom;
                                transferData.currency = request.Currency;
                                var type = i == 0 ? "TRANSFER SETOR" : "TRANSFER TARIK";

                                if (transferData.amount < 0)
                                {
                                    throw new Exception("amount tidak boleh kurang dari 0");
                                }
                                //check apakah data history terakhir punya tanggal, currency dan tipe yang sama
                                var counterHistory = false;
                                var counterReset = false;
                                var stringCheck = $"SELECT TOP 1 * FROM [BOS_History] order by szTransactionId desc";
                                using (var checkCommand = new OleDbCommand(stringCheck, connection, transaction))
                                using (var reader = checkCommand.ExecuteReader())
                                {
                                    var row = new Dictionary<string, object>();
                                    while (reader.Read())
                                    {
                                        for (int j = 0; j < reader.FieldCount; j++)
                                        {
                                            row[reader.GetName(j)] = reader.IsDBNull(j) ? null : reader.GetValue(j);
                                        }
                                    }

                                    var checkDate = DateTime.Parse(row["dtmTransaction"].ToString());
                                    if (DateTime.Now.Date != checkDate.Date) counterReset = true;
                                    else if (row["szAccountId"].Equals(transferData.account) && !row["szCurrencyId"].Equals(transferData.currency))
                                    {
                                        counterHistory = true;
                                    }

                                    BOS_History bosHist = new BOS_History();
                                    bosHist.dtmTransaction = DateTime.Now;
                                    bosHist.decAmount = type.Contains("SETOR") ? transferData.amount : -1 * transferData.amount;
                                    bosHist.szNote = type.Contains("TRANSFER") ? "TRANSFER" : type;
                                    bosHist.szAccountId = transferData.account;
                                    bosHist.szCurrencyId = transferData.currency;
                                    if (counterHistory)
                                    {
                                        bosHist.szTransactionId = row["szTransactionId"].ToString();
                                    }
                                    else
                                    {
                                        if(i==0) lastNumber = _counterService.GetTransactionIdLastNumber(connection, transaction);
                                        if (i==0 && counterReset) lastNumber = 0; // reset ke 0 di hari berbeda
                                        lastNumber++;
                                        bosHist.szTransactionId = _counterService.CreateTransactionId(lastNumber);
                                    }

                                    // insert to bos history
                                    var status = _historyService.InsertToBosHistory(bosHist, connection, transaction);
                                    if (status.Equals("Success"))
                                    {
                                        BOS_Balance bosBal = new BOS_Balance();
                                        bosBal.szAccountId = transferData.account;
                                        bosBal.szCurrencyId = transferData.currency;
                                        bosBal.decAmount = transferData.amount;
                                        // update bos balance
                                        var statusBalance = _balanceService.UpdateBosBalance(bosBal, type, connection, transaction);
                                        if (statusBalance.Equals("Success"))
                                        {
                                            response.Status = "Success";
                                            response.message = type.Equals("SETOR") ? "Setor Success" : "Tarik Success";
                                            if (!counterHistory) _counterService.GetAndUpdateTransactionIdLastNumber(lastNumber, connection, transaction);
                                        }
                                        else throw new Exception("Failed ketika Update Balance");
                                    }
                                    else throw new Exception("Failed ketika Update History");
                                }
                                message += i == 0 ? "Transfer ke " + transferData.account + " sebanyak " + transferData.amount + " berhasil dilakukan; " :
                                    "Penarikan dari " + transferData.account + " sebanyak " + transferData.amount + " berhasil dilakukan; ";
                            }

                            transaction.Commit();
                            response.amount = 0;
                            response.currency = "-";
                            response.Status = "Success";
                            response.message = message;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.Status = "Failed, error : " + ex.Message;
                            response.message = "Terjadi kesalahan ketika transfer ke " + request.AccountTo;
                        }
                    }
                }
            }
            return response;
        }

    }
}
