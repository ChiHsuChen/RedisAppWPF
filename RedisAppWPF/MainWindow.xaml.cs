/*
 * Purpose: a simple demo application with connecting to Redis (WPF Version)
 * Author: Chi-Hsu Chen; css920@gmail.com
 * Date: 20171208 
 */

using StackExchange.Redis;
using System;
using System.Windows;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisAppWPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectionMultiplexer myRedisConn;
        ISubscriber myChannel_1;
        IDatabase myRedisDB;
        private delegate void ShowMessage(string lsMessage);
        
        struct rawdata
        {
            public string chartid;
            public string waferid;
            public string pointid;
            public double value;
        };

        struct PointData
        {
            public string chartid;
            public double xbar;
            public double sigma;
            public double range;
            public int chartseq;
            public List<rawdata> lstRawData;
        };

        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowLog(string lsMessage)
        {
            lstMessage.Items.Add(DateTime.Now.Year + "/" +
                DateTime.Now.Month + "/" +
                DateTime.Now.Day + " " +
                DateTime.Now.Hour + ":" +
                DateTime.Now.Minute + ":" +
                DateTime.Now.Second + "." +
                DateTime.Now.Millisecond + " " + lsMessage);
            lstMessage.SelectedIndex = lstMessage.Items.Count - 1;
            lstMessage.Items.MoveCurrentTo(lstMessage.SelectedIndex);
        }

        private void myChannel1Handler(RedisChannel lsChannel, RedisValue lsValue)
        {
            // using delegate function to avoid not in the same thread exception
            // in WPF, use Dispatcher.Invoke
            ShowMessage WriteLog = new ShowMessage(ShowLog);
            Dispatcher.Invoke(WriteLog, "received on channel[" + lsChannel.ToString() + "] - " + lsValue.ToString());
        }

        // ========================================================
        // ========================================================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            myRedisConn = ConnectionMultiplexer.Connect("192.168.234.128:6379");
            myChannel_1 = myRedisConn.GetSubscriber();
            myRedisDB = myRedisConn.GetDatabase();

            this.Title = "RedisApp - " + "V." + System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            txtKeyToStore.Text = "REDIS:TEST:APP:KEY1";
            txtExpireTime.Text = "1200";
            //toolStripStatusLabel1.Text = "Connect to Redis @ 192.168.234.128:6379 OK";
            ShowLog("Connect to Redis @ 192.168.234.128:6379 OK");
        }

        private void cmdIncrTest_Click(object sender, RoutedEventArgs e)
        {
            int lsStartTick;
            int lsEndTick;

            lsStartTick = System.Environment.TickCount;
            myRedisDB.StringIncrement(txtKeyToStore.Text, 1);
            lsEndTick = System.Environment.TickCount;
            ShowLog("incr key[" + txtKeyToStore.Text + "] by 1. elapsed time=" + (lsEndTick - lsStartTick).ToString() + "ms");

            ShowLog("incr key[" + txtKeyToStore.Text + "] by 1. value = " + myRedisDB.StringGet(txtKeyToStore.Text));
            myChannel_1.Publish(txtChannel.Text, "incr key[" + txtKeyToStore.Text + "] by 1");

        }

        private void cmdDeleteItemByKey_Click(object sender, RoutedEventArgs e)
        {
            if (myRedisDB.KeyExists(txtKeyToStore.Text))
            {
                myRedisDB.KeyDelete(txtKeyToStore.Text);
                ShowLog("key[" + txtKeyToStore.Text + "] deleted");
            }
            else
            {
                ShowLog("key[" + txtKeyToStore.Text + "] not exist. SKIP ...");
            }
        }

        private void cmdCacheToRedis_Click(object sender, RoutedEventArgs e)
        {
            if (!myRedisDB.KeyExists(txtKeyToStore.Text.Trim()))
            {
                if (txtKeyToStore.Text.Trim() != "" && txtDataToStore.Text.Trim() != "")
                {
                    myRedisDB.StringSet(txtKeyToStore.Text.Trim(), txtDataToStore.Text.Trim());
                }
            }

            if (txtExpireTime.Text != "")
            {
                TimeSpan lsTime = new TimeSpan(0, 0, Int32.Parse(txtExpireTime.Text));
                myRedisDB.KeyExpire(txtKeyToStore.Text, lsTime);
                ShowLog("set expire time on [" + txtKeyToStore.Text + "] to " + lsTime.TotalSeconds);
            }

            myChannel_1.Publish(txtChannel.Text, "DATASTORE_EVENT { KEY " + txtKeyToStore.Text + " } { VALUE " + txtDataToStore.Text + " }");
            ShowLog("[cmdCacheToRedis_Click] add key=" + txtKeyToStore.Text.Trim() + ";value=" + txtDataToStore.Text.Trim());
            ShowLog("publish on channel[" + txtChannel.Text + "] - DATASTORE_EVENT { KEY " + txtKeyToStore.Text + " } { VALUE " + txtDataToStore.Text + " }");

        }

        private void cmdSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (txtChannel.Text.Trim() != "")
            {
                myChannel_1.Subscribe(txtChannel.Text, myChannel1Handler);
                ShowLog("Subscribe on channel[" + txtChannel.Text + "]");
                //myChannel_1.Subscribe(txtChannel.Text, (cannel, value) =>
                //{
                //    Console.WriteLine("Receiving from " + cannel.ToString() + ";msg=" + value.ToString());
                //});
            }
        }

        private void cmdPublish_Click(object sender, RoutedEventArgs e)
        {
            myChannel_1.Publish(txtChannel.Text, txtMessage.Text);
            ShowLog("publish on channel[" + txtChannel.Text + "] - " + txtMessage.Text);
        }

        private void cmdSetExpireTime_Click(object sender, RoutedEventArgs e)
        {
            if (txtExpireTime.Text != "")
            {
                TimeSpan lsTime = new TimeSpan(0, 0, Int32.Parse(txtExpireTime.Text));
                myRedisDB.KeyExpire(txtKeyToStore.Text, lsTime);
                ShowLog("set expire time on [" + txtKeyToStore.Text + "] to " + lsTime.TotalSeconds);
            }
                
        }

        private void cmdShowTTL_Click(object sender, RoutedEventArgs e)
        {
            ShowLog("TTL on [" + txtKeyToStore.Text + "] is " + myRedisDB.KeyTimeToLive(txtKeyToStore.Text));
        }

        private void cmdAddToSet_Click(object sender, RoutedEventArgs e)
        {
            myRedisDB.SetAdd(txtKeyToStore.Text, txtDataToStore.Text);
            if (txtExpireTime.Text != "")
            {
                TimeSpan lsTime = new TimeSpan(0, 0, Int32.Parse(txtExpireTime.Text));
                myRedisDB.KeyExpire(txtKeyToStore.Text, lsTime);
                ShowLog("set expire time on [" + txtKeyToStore.Text + "] to " + lsTime.TotalSeconds);
            }
            ShowLog("add to set[" + txtKeyToStore.Text + "] value = " + txtDataToStore.Text);
            myChannel_1.Publish(txtChannel.Text, "add to set[" + txtKeyToStore.Text + "] value = " + txtDataToStore.Text);
            ShowLog("add to set[" + txtKeyToStore.Text + "] value = " + txtDataToStore.Text + " publish on cannel[" + txtChannel.Text + "]");
        }

        private void cmdDumpSet_Click(object sender, RoutedEventArgs e)
        {
            if (!myRedisDB.KeyExists(txtKeyToStore.Text))
                return;
            
            foreach (RedisValue tValue in myRedisDB.SetMembers(txtKeyToStore.Text))
            {
                ShowLog("member in set[" + txtKeyToStore.Text + "] = " + tValue.ToString());
            }
        }

        private void cmdJsonTest_Click(object sender, RoutedEventArgs e)
        {
            // 1. Serize Object to JSON format
            PointData p1 = new PointData();

            rawdata rd1 = new rawdata();
            rd1.chartid = "1";
            rd1.pointid = "1";
            rd1.waferid = "TEST.1";
            rd1.value = 20.12324;

            rawdata rd2 = new rawdata();
            rd2.chartid = "1";
            rd2.pointid = "2";
            rd2.waferid = "TEST.1";
            rd2.value = 18.12324;

            p1.chartid = "1";
            p1.range = 0;
            p1.sigma = 0;
            p1.xbar = 0;
            p1.chartseq = 1;
            p1.lstRawData = new List<rawdata>();
            p1.lstRawData.Add(rd1);
            p1.lstRawData.Add(rd2);

            string jsonValue1 = JsonConvert.SerializeObject(p1);
            myRedisDB.SetAdd(txtKeyToStore.Text, jsonValue1);
            ShowLog("Json Test-add key[" + txtKeyToStore.Text + "]. data in json=" + jsonValue1);

            PointData p2 = new PointData();

            rawdata rd3 = new rawdata();
            rd3.chartid = "1";
            rd3.pointid = "3";
            rd3.waferid = "TEST.2";
            rd3.value = 2.12324;

            rawdata rd4 = new rawdata();
            rd3.chartid = "1";
            rd3.pointid = "4";
            rd3.waferid = "TEST.2";
            rd3.value = 1.12324;
            p2.chartid = "1";
            p2.range = 1;
            p2.sigma = 1;
            p2.xbar = 1;
            p2.chartseq = 2;
            p2.lstRawData = new List<rawdata>();
            p2.lstRawData.Add(rd3);
            p2.lstRawData.Add(rd4);

            string jsonValue2 = JsonConvert.SerializeObject(p2);
            myRedisDB.SetAdd(txtKeyToStore.Text, jsonValue2);
            ShowLog("Json Test-add key[" + txtKeyToStore.Text + "]. data in json=" + jsonValue2);

            // 2. De-serize JSON data to object
            if (!myRedisDB.KeyExists(txtKeyToStore.Text))
                return;

            foreach (RedisValue tValue in myRedisDB.SetMembers(txtKeyToStore.Text))
            {
                ShowLog("member in set[" + txtKeyToStore.Text + "] JSON= " + tValue.ToString());
                PointData jsondata = JsonConvert.DeserializeObject<PointData>(tValue.ToString());
                ShowLog("de-serize complete-jsondata=" + jsondata.ToString());
            }
        }

        private void cmdTransactionTest_Click(object sender, RoutedEventArgs e)
        {
            var trans = myRedisDB.CreateTransaction();
            
            for (int i = 0; i <= 100; i++)
            {
                trans.SetAddAsync(txtKeyToStore.Text, i);
            }
            
            if (trans.Execute())
                ShowLog("cmdTransactionTest_Click - transaction test OK");
            else
                ShowLog("cmdTransactionTest_Click - transaction test Fail");
        }

        private void cmdGetByKey_Click(object sender, RoutedEventArgs e)
        {
            string value;
            value = myRedisDB.StringGet(txtKeyToStore.Text);
            ShowLog("cmdGetByKey_Click get by key[" + txtKeyToStore.Text + "] = " + value);
        }

        private void cmdIncrStressTest_Click(object sender, RoutedEventArgs e)
        {
            int lsStartTick;
            int lsEndTick;
            int lsRounds = 10000;

            lsStartTick = System.Environment.TickCount;
            for (int i=1;i<= lsRounds; i++)
            {
                myRedisDB.StringIncrementAsync(txtKeyToStore.Text, 1);
            }
            lsEndTick = System.Environment.TickCount;
            ShowLog("StringIncrementAsync key[" + txtKeyToStore.Text + "] by 1 for " + lsRounds.ToString() + " rounds. elapsed time=" + (lsEndTick - lsStartTick).ToString() + "ms");
            ShowLog(txtKeyToStore.Text + " get value is " + myRedisDB.StringGet(txtKeyToStore.Text));

            lsStartTick = System.Environment.TickCount;
            for (int i = 1; i <= lsRounds; i++)
            {
                myRedisDB.StringIncrement(txtKeyToStore.Text, 1);
            }
            lsEndTick = System.Environment.TickCount;
            ShowLog("StringIncrement key[" + txtKeyToStore.Text + "] by 1 for " + lsRounds.ToString() + " rounds. elapsed time=" + (lsEndTick - lsStartTick).ToString() + "ms");
            ShowLog(txtKeyToStore.Text + " get value is " + myRedisDB.StringGet(txtKeyToStore.Text));

            lsStartTick = System.Environment.TickCount;
            IncrAsync();
            ShowLog("local loop. elapsed time=" + (lsEndTick - lsStartTick).ToString() + "ms");
            lsEndTick = System.Environment.TickCount;
        }

        private async Task<long> IncrAsync()
        {
            long lsResult=0;

            for (int i = 1; i <= 10000; i++)
            {
                lsResult++;
            }
                        
            return lsResult;
        }
    }
}
