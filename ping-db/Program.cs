using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PingDatabase
{
    class Program
    {
        private static readonly Random Random = new Random();
        private static ulong _tryCount;
        private static ulong _successCount;
        private static uint _minTimeMs = uint.MaxValue;
        private static uint _maxTimeMs;
        private static ulong _totalTime;
        private const string PasswordOption = "Password";
        private static readonly string[] RequiredConnectionOptions = { "Server", "Database", "User ID", PasswordOption };
        //private static DatabaseConnection _connection;
        private static List<DatabaseConnection> _possible_connections = new List<DatabaseConnection>();

        /// <summary>
        /// Sends SQL commands to a SQL server.
        /// </summary>
        /// <param name="connectionString">Connection options in dotnet format. Server, Database, UserID and Password will be prompted to enter if they are not specified.
        /// Note that Connection Pooling is enabled by default, please use the corresponding connection option if you would like to disable it.
        /// Connection Options Reference: https://mysqlconnector.net/connection-options/ .</param>
        /// <param name="maxCount">Limit number of pings.</param>
        /// <param name="delayMs">Delay in ms. Default value is 1000.</param>
        /// <param name="payloadSize">Number of text symbols to send and receive the @payload parameter. Default value is 1.</param>
        /// <param name="commandText">SQL command to execute. Command must return the @payload parameter. Default value is "select @payload"</param>
        static void Main(string connectionString=null, ulong maxCount = ulong.MaxValue, uint delayMs = 1000,
            int payloadSize = 1, string commandText = "select @payload")
        {
            try
            {
                _possible_connections.Add(new MsSqlConnectionImpl(connectionString));
            }
            catch {}
            try
            {
                _possible_connections.Add(new PgSqlConnectionImpl(connectionString));
            }
            catch { }
            try
            {
                _possible_connections.Add(new MysqlConnectionImpl(connectionString));
            }
            catch { }

            _possible_connections = PromptConnectionOptions(_possible_connections);
             
            string payload = RandomString(payloadSize);

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("");
                cts.Cancel();
                e.Cancel = true;
            };

           
            Console.WriteLine($"PING {_possible_connections.First().Server} ({_possible_connections.First().Database})");
            try
            {
                Ping(maxCount, delayMs, commandText, payload, cts)
                        .GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            { }

            PrintStatistics();
        }

        private static async Task Ping(ulong maxCount, uint delayMs, string commandText, string payload,
            CancellationTokenSource cts)
        {
            DatabaseConnection connection = null;
            var stopwatch = new Stopwatch();
            Console.WriteLine("Checking connection... ");
            try
            {
                stopwatch.Start();
                
                var tasks = _possible_connections.Select(
                    c => c.CheckConnection(cts.Token))
                    .ToArray();
                connection = await WaitForFirstSuccess(tasks);
                
                stopwatch.Stop();
                Console.WriteLine($"Connected to {connection.ServerType} in {stopwatch.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                PrintFail(ex);
                return;
            }

            while (_tryCount < maxCount && !cts.IsCancellationRequested)
            {
                try
                {
                    
                    stopwatch.Restart();
                    var value = await connection.Ping(commandText, payload, cts.Token);
                    stopwatch.Stop();
                    var timeMs = (uint) stopwatch.ElapsedMilliseconds;
                    _tryCount++;
                    _successCount++;
                    _totalTime += timeMs;
                    _maxTimeMs = Math.Max(_maxTimeMs, timeMs);
                    _minTimeMs = Math.Min(_minTimeMs, timeMs);

                    var isResponseValid = payload.Equals(value);
                    PrintSuccess(payload.Length, timeMs, isResponseValid);
                }
                catch (Exception e)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        _tryCount++;
                        PrintFail(e);
                    }
                }

                if (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cts.Token);
                }
            }
        }

        private static async Task<T> WaitForFirstSuccess<T>(Task<T>[] tasks)
        {
            var taskList = new List<Task<T>>(tasks);
            while (taskList.Count > 0)
            {
                var task = await Task.WhenAny(taskList);
                if (task.IsFaulted)
                    taskList.Remove(task);
                else
                {
                    return task.Result;
                }
            }

            // all tasks failed, throw exception;
            await Task.WhenAll(tasks);
            return default(T);
        }

        private static List<DatabaseConnection> PromptConnectionOptions(List<DatabaseConnection> possible_connections)
        {
            foreach (var option in RequiredConnectionOptions)
            {
                var withOption = possible_connections.Where(c => c.ContainsOption(option)).ToList();
                if (withOption.Any())
                {
                    possible_connections = withOption;
                }
                else
                {
                    var prompt = $"{option}:";
                    var value = option == PasswordOption
                        ? ReadLine.ReadPassword(prompt) 
                        : ReadLine.Read(prompt);
                    foreach (var connection in possible_connections)
                    {
                        connection.SetOption(option, value);
                    }
                }
            }

            return possible_connections;
        }

       

        private static void PrintSuccess(int payloadSize, uint timeMs, bool isResponseValid)
        {
            var warnText = isResponseValid ? string.Empty : "WARN: Received payload does not equal to sent payload.";
            Console.WriteLine($"{payloadSize} symbols from {_possible_connections.First().Server} time={timeMs} ms.{warnText}");
        }

        private static void PrintFail(Exception e)
        {
            string innerMessage = String.Empty;
            if (e.InnerException != null)
            {
                innerMessage = e.InnerException.Message;
                if (string.IsNullOrEmpty(innerMessage))
                {
                    innerMessage = e.InnerException.GetType().Name;
                }
            }

            var innerText = string.IsNullOrEmpty(innerMessage) || innerMessage == e.Message ? string.Empty : $" ({innerMessage})";
            Console.WriteLine(e.Message + innerText);
        }

        private static void PrintStatistics()
        {
            Console.WriteLine($"--- {_possible_connections.First().Server} ping statistics ---");
            if (_tryCount > 0)
            {
                var failedRate = 100 - 100.0 * _successCount / _tryCount;
                Console.WriteLine($"{_tryCount} SQL commands sent, {_successCount} commands succeeded, {failedRate}% failed.");

                if (_successCount > 0)
                {
                    var avgTimeMs = _totalTime / _successCount;
                    Console.WriteLine($"round-trip min/avg/max = {_minTimeMs}/{avgTimeMs}/{_maxTimeMs} ms");
                }
            }
            else
            {
                Console.WriteLine($"{_tryCount} SQL commands sent, {_successCount} commands succeeded.");
            }
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
