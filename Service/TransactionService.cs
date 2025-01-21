using BosnetTest.Model.dto;
using System.Data.OleDb;
using System.Data;
using BosnetTest.Model;
using System.Diagnostics.Metrics;
using static BosnetTest.Model.dto.TransactionHistoryResponse;

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

        public TransactionTransferResponse Transfer(TransactionTransferRequest requestList)
        {
            TransactionTransferResponse response = new TransactionTransferResponse();
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
                                transferData.account = i == 0 ? request.AccountTo : requestList.AccountFrom;
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

        public TransactionHistoryResponse GetTransactionHistory(TransactionHistoryRequest request)
        {
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                    {
                        TransactionHistoryResponse response = new TransactionHistoryResponse();
                        try
                        {
                            List<TransactionHistoryData> results = new List<TransactionHistoryData>();
                            var datas = _historyService.GetTransactionHistory(request, connection, transaction);
                            foreach (var data in datas)
                            {
                                TransactionHistoryData d = new TransactionHistoryData();
                                d.Account = data["szAccountId"].ToString();
                                d.Currency = data["szCurrencyId"].ToString();
                                d.TransactionType = data["szNote"].ToString();
                                d.Amount = decimal.Parse(data["decAmount"].ToString());
                                d.Date = data["dtmTransaction"].ToString();
                                results.Add(d);
                            }

                            
                            response.histories = results;
                            response.status = "Success";
                            response.message = "Data transaksi history berhasil diambil";
                            transaction.Commit();

                            return response;
                        }
                        catch(Exception ex)
                        {
                            response.status = "Failed";
                            response.message = "Data transaksi history gagal diambil : " + ex.Message;
                            transaction.Rollback();
                            return response;
                        }

                    }
                }
            }
        }

    }
}
