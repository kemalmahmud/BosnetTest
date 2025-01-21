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
                            bosHist.decAmount = type.Contains("Setor") ? request.amount : -1 * request.amount;
                            bosHist.szNote = type.Contains("Transfer") ? "Transfer" : type;
                            bosHist.szAccountId = request.account;
                            bosHist.szCurrencyId = request.currency;
                            if(counterHistory)
                            {
                                bosHist.szTransactionId = row["szTransactionId"].ToString();
                            }
                            else
                            {
                                int lastNumber = _counterService.GetTransactionIdLastNumber(connection, transaction);
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
                                    response.status = "Success";
                                    response.message = "Deposit Success";
                                    if(!counterHistory) _counterService.GetAndUpdateTransactionIdLastNumber(counterReset, connection, transaction);
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
                        response.status = "Failed, error : " + ex.Message;
                        response.message = "Deposit Failed";
                        return response;
                    }
                }
            }
        }

    }
}
