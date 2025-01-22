using BosnetTest.Model.dto;
using System.Data.OleDb;
using System.Data;
using BosnetTest.Model;
using static BosnetTest.Model.dto.TransactionHistoryResponse;
using static BosnetTest.Model.dto.TransactionRequest;

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

        public TransactionResponse SetorAtauTarik(TransactionRequest requestList, string type)
        {
            // grouping same currency
            groupSameCurrency(ref requestList);
            TransactionResponse response = new TransactionResponse();
            response.type = type;
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    var message = "";
                    try
                    {
                        int lastNumber = 0; // last counter number
                        foreach (var request in requestList.transactions)
                        {
                            try
                            {
                                if (request.amount < 0)
                                {
                                    throw new Exception("amount tidak boleh kurang dari 0");
                                }
                                //check apakah data history terakhir punya tanggal, currency dan tipe yang sama
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

                                    BOS_History bosHist = new BOS_History();
                                    bosHist.dtmTransaction = DateTime.Now;
                                    bosHist.decAmount = type.Contains("SETOR") ? request.amount : -1 * request.amount;
                                    bosHist.szNote = type.Contains("TRANSFER") ? "TRANSFER" : type;
                                    bosHist.szAccountId = requestList.account;
                                    bosHist.szCurrencyId = request.currency;
                                    lastNumber = _counterService.GetTransactionIdLastNumber(connection, transaction);
                                    if (counterReset) lastNumber = 0; // reset ke 0 di hari berbeda
                                    lastNumber++;
                                    bosHist.szTransactionId = _counterService.CreateTransactionId(lastNumber);

                                    // insert to bos history
                                    var status = _historyService.InsertToBosHistory(bosHist, connection, transaction);
                                    if (status.Equals("Success"))
                                    {
                                        BOS_Balance bosBal = new BOS_Balance();
                                        bosBal.szAccountId = requestList.account;
                                        bosBal.szCurrencyId = request.currency;
                                        bosBal.decAmount = request.amount;
                                        // update bos balance
                                        var statusBalance = _balanceService.UpdateBosBalance(bosBal, type, connection, transaction);
                                        if (!statusBalance.Equals("Success"))
                                        {
                                            throw new Exception("Failed ketika Update Balance");
                                        }
                                    }
                                    else throw new Exception("Failed ketika Update History");
                                }
                                message += $"Transaksi dengan amount {request.currency}{request.amount} berhasil; ";
                            } 
                            catch (Exception ex)
                            {
                                message += $"Transaksi dengan amount {request.currency}{request.amount} gagal, alasan : {ex.Message}; ";
                                throw new Exception(message, ex);
                            }
                        }
                        _counterService.GetAndUpdateTransactionIdLastNumber(lastNumber, connection, transaction);
                        response.status = "Success";
                        response.message = message;
                        transaction.Commit();
                        return response;

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.message = "Failed, error : " + ex.Message;
                        if(type.Equals("SETOR")) response.status = "Setor Failed";
                        if (type.Equals("TARIK")) response.status = "Tarik Failed";
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
            foreach (var request in requestList.transfers)
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
                                TransactionRequestData transferData = new TransactionRequestData();
                                transferData.amount = request.amount;
                                var account = i == 0 ? request.accountTo : requestList.accountFrom;
                                transferData.currency = request.currency;
                                var type = i == 0 ? "TRANSFER SETOR" : "TRANSFER TARIK";

                                if (transferData.amount < 0)
                                {
                                    throw new Exception("amount tidak boleh kurang dari 0");
                                }
                                
                                //check apakah sudah ganti hari
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

                                    BOS_History bosHist = new BOS_History();
                                    bosHist.dtmTransaction = DateTime.Now;
                                    bosHist.decAmount = type.Contains("SETOR") ? transferData.amount : -1 * transferData.amount;
                                    bosHist.szNote = type.Contains("TRANSFER") ? "TRANSFER" : type;
                                    bosHist.szAccountId = account;
                                    bosHist.szCurrencyId = transferData.currency;

                                    //get last number
                                    lastNumber = _counterService.GetTransactionIdLastNumber(connection, transaction);
                                    if (counterReset) lastNumber = 0; // reset ke 0 di hari berbeda
                                    lastNumber++;
                                    bosHist.szTransactionId = _counterService.CreateTransactionId(lastNumber);

                                    // insert to bos history
                                    var status = _historyService.InsertToBosHistory(bosHist, connection, transaction);
                                    if (status.Equals("Success"))
                                    {
                                        BOS_Balance bosBal = new BOS_Balance();
                                        bosBal.szAccountId = account;
                                        bosBal.szCurrencyId = transferData.currency;
                                        bosBal.decAmount = transferData.amount;
                                        // update bos balance
                                        var statusBalance = _balanceService.UpdateBosBalance(bosBal, type, connection, transaction);
                                        if (!statusBalance.Equals("Success"))
                                        {
                                            throw new Exception("Failed ketika Update Balance");

                                        }
                                    }
                                    else throw new Exception("Failed ketika Update History");
                                }
                                message += i == 0 ? "Transfer ke " + account + " sebanyak " + transferData.amount + " berhasil dilakukan; " :
                                    "Penarikan dari " + account + " sebanyak " + transferData.amount + " berhasil dilakukan; ";
                            }

                            _counterService.GetAndUpdateTransactionIdLastNumber(lastNumber, connection, transaction);
                            transaction.Commit();
                            response.status = "Success";
                            response.message = message;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.status = "Failed";
                            response.message = $"Terjadi kesalahan ketika transfer ke {request.accountTo}, error : {ex.Message}; ";
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
                                d.account = data["szAccountId"].ToString();
                                d.currency = data["szCurrencyId"].ToString();
                                d.transactionType = data["szNote"].ToString();
                                d.amount = decimal.Parse(data["decAmount"].ToString());
                                d.date = data["dtmTransaction"].ToString();
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

        private void groupSameCurrency(ref TransactionRequest request)
        {
            var groupedTransactions = request.transactions
            .GroupBy(t => t.currency)
            .Select(g => new TransactionRequestData
            {
                currency = g.Key,
                amount = g.Sum(t => t.amount)
            })
            .ToList();

            // Update request dengan grup currency
            request.transactions = groupedTransactions;
        }

    }
}
