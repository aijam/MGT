using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using OpcUaHelper;

/*
 @Copyright JUNGHEINRICH V8CN 2020.11.13
    */

namespace MGT
{
    public partial class Form1 : Form
    {
        private System.Threading.Timer timer1, timer2, timer3, timer4, timer5;

        Boolean stationRelease = false; //是否接收站台的搬送请求；当收到输送机的前一个物料类型时，将此字段置为true，直到AGV取货完成后，才置为false，然后sleep 10s后才能接收新的物料类型
        Boolean autoUpdateJobStatus = true; //是否自动更新任务状态，从5到6，从8到9等
        Boolean connectedWithOPC = false; //是否连接opcserver，当opcserver宕机的时候，需要uncheck

        private OpcUaClient opcUaClient = new OpcUaClient();

        //const
        int jobType_1 = 1;
        int jobType_2 = 2;

        public Form1()
        {
            InitializeComponent();
            // 不检查线程之间的冲突
            Control.CheckForIllegalCrossThreadCalls = false;

            opcUaClient.ConnectServer("opc.tcp://127.0.0.1:49320");
            timer1 = new System.Threading.Timer(new TimerCallback(timer1_updateJobStatus), null, 0, 1000);
            timer2 = new System.Threading.Timer(new TimerCallback(timer2_updateAGVWorkStatus), null, 0, 3000);
            timer3 = new System.Threading.Timer(new TimerCallback(timer3_GUIStationStautus), null, 0, 5000);
            timer4 = new System.Threading.Timer(new TimerCallback(timer_opc_read), null, 0, 5000);
            timer5 = new System.Threading.Timer(new TimerCallback(enable_opc_read), null, 0, 1000);
        }

        //模拟输送机的信号，A物料已就绪
        private void CallA_Click(object sender, EventArgs e)
        {
            abc(1);
        }

        //模拟输送机的信号，B物料已就绪
        private void CallB_Click(object sender, EventArgs e)
        {
            abc(2);
        }

        private void updateGUIStationStautus(Control cc)
        {

            using (var ctx = new AMSContext())
            {
                foreach (System.Windows.Forms.Control control in cc.Controls)
                {
                    if (control is System.Windows.Forms.Button)
                    {
                        System.Windows.Forms.Button btn = (System.Windows.Forms.Button)control;
                        if (btn.Text != "Reset")
                        {
                            int stationNo = int.Parse(btn.Text);
                            t_Station agvStation = ctx.t_Station
                                  .Where(b => b.StationNo == stationNo)
                                  .SingleOrDefault();

                            if (agvStation.OccupiedStatus == 0)
                            {
                                btn.BackColor = Color.WhiteSmoke;
                            }
                            else if (agvStation.OccupiedStatus == 1)
                            {
                                btn.BackColor = Color.Yellow;
                            }
                            else if (agvStation.OccupiedStatus == 2)
                            {
                                btn.BackColor = Color.Blue;
                            }
                        }

                        if (control.Controls.Count > 0)
                        {
                            updateGUIStationStautus(control);
                        }
                    }
                }
            }
        }

        //触发任务
        private Boolean triggerTask(int callButtonSignal, int stationNo)
        {
            t_AGVPath agvPath1;
            t_Station agvStation1;

            //根据规则找到找路径，一般只能找到一条，如果找到多条，按照规则选一条
            using (var ctx = new AMSContext())
            {
                //read
                agvStation1 = ctx.t_Station
                .Where(b => b.StationNo == stationNo)
                .SingleOrDefault();

                agvPath1 = getPath(agvStation1, callButtonSignal);
            }

            if (agvPath1 == null)
            {
                Console.WriteLine(DateTime.Now.ToString() + " triggerTask：根据" + agvStation1.StationNo + " 和拉动信号类型 " + callButtonSignal + " 找不到对应的路径");
                return false;
            }

            //如果路径下有多个站台，按照策略选一个站台
            t_Station agvStation2 = getAvailabeStation(agvPath1);
            //构建任务实体
            if (agvStation2 != null)
            {
                t_AGVWork agvWork1 = new t_AGVWork
                {
                    JobType = 0,
                    Origination = agvStation1.StationNo,
                    Destination = agvStation2.StationNo,
                    //JobId = 10000,
                    Priority = 0,
                    JobStatus = 0,
                    TUType = 0,
                    AGVCancelFlag = 0,
                    CancelFlag = 0,
                    WMSCancelFlag = 0,
                    RedirectFlag = 0
                };
                //创建任务并更新货位状态（货位预约，空满状态 0: 空 1:预约中 2:满）
                if (createTask(agvWork1))
                {
                    //更新界面颜色,将对应货位的颜色改为黄色(预约中）
                    //updateGUIStationStautus(agvStation1, 1);

                    stationRelease = false;
                    checkbox_stationRelease.Checked = false; //站台不可再接收其他的搬运指令
                    checkbox_stationRelease.Enabled = false; //而且不允许更改
                }
                else
                {
                    toolStripStatusLabel1.Text = "创建任务失败";
                    Console.WriteLine(DateTime.Now.ToString() + " triggerTask：根据起始站台： " + agvStation1.StationNo + " 和目的站台 " + agvStation2.StationNo + " 创建任务失败");
                    return false;
                }
                //更新路径上任务数量-本次不实现（控制一条路径上的流量）
            }
            else
            {
                //toolStripStatusLabel1.Text = "无可用站台";
                Console.WriteLine(DateTime.Now.ToString() + " triggerTask：根据起始站台： " + stationNo + " 和目的通道 " + agvPath1.Destination + " 找不到可用的站台");
                return false;
            }

            return true;
        }

        //创建任务-事务，插入作业任务agvwork数据和更新站台状态agvstation在同一个事务内完成
        private Boolean createTask(t_AGVWork agvWork2)
        {
            t_Station agvStation1;
            //创建任务
            using (var ctx = new AMSContext())
            {
                //检查是否有目的地为同一个站台的任务还在执行中
                List<t_AGVWork> agvWorks = ctx.t_AGVWork
                .Where(b => b.Destination == agvWork2.Destination)
                .ToList();
                //Console.WriteLine(agvWork1);

                if (agvWorks.Count > 0)
                {
                    toolStripStatusLabel1.Text = DateTime.Now.ToString() + " 已经存在一条目的地为" + agvWork2.Destination + "的任务了，不能重复创建";
                    Console.WriteLine(DateTime.Now.ToString() + " createTask：已经存在一条目的地为" + agvWork2.Destination + "的任务了，不能重复创建");
                    return false;
                }

                var transaction = ctx.Database.BeginTransaction();
                try
                {
                    agvWork2.ModifyProgID = 101;
                    agvWork2.ModifyTime = DateTime.Now;
                    ctx.t_AGVWork.Add(agvWork2); //插入agv作业任务
                    ctx.SaveChanges();

                    agvWork2.JobId = agvWork2.ID;
                    var setEntry = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(agvWork2);
                    setEntry.SetModifiedProperty("JobId");
                    ctx.SaveChanges();

                    //更新站台状态为预约
                    agvStation1 = ctx.t_Station
                    .Where(b => b.StationNo == agvWork2.Destination)
                    .First();
                    agvStation1.OccupiedStatus = 1; //空满状态 0: 空 1:预约中 2:满
                    agvStation1.ModifyProgID = 101;
                    agvStation1.ModifyTime = DateTime.Now;
                    ctx.t_Station.Attach(agvStation1);
                    var setEntry1 = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(agvStation1);
                    setEntry1.SetModifiedProperty("OccupiedStatus");
                    setEntry1.SetModifiedProperty("ModifyProgID");
                    setEntry1.SetModifiedProperty("ModifyTime");
                    ctx.SaveChanges();

                    transaction.Commit();

                    Console.WriteLine(DateTime.Now.ToString() + " createTask：任务" + agvWork2.ID + " 已创建，站台 " + agvWork2.Destination + " 状态更新为(预约中)");
                    toolStripStatusLabel1.Text = " 从 " + agvWork2.Origination + " 到 " + agvWork2.Destination + " 的搬运任务已创建";

                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    Console.WriteLine(DateTime.Now.ToString() + " createTask：失败了，事务回滚");
                }
                finally
                {
                    transaction.Dispose();
                }
            }

            return false;
        }

        //计算可用路径-业务相关-根据起点找到所有可用路径
        private t_AGVPath getPath(t_Station originalStation, int callButtonSignal)
        {
            t_AGVPath agvPath1;

            using (var ctx = new AMSContext())
            {
                //read
                agvPath1 = ctx.t_AGVPath
                    .Where(b => b.AvailableStatus == 1 && b.Origination == originalStation.StationNo && b.ChannelType == callButtonSignal)
                    .First();
                Console.WriteLine(DateTime.Now.ToString() + " 找到对应的通道 " + agvPath1.Destination);

            }
            return agvPath1;
        }


        //计算可用货位
        private t_Station getAvailabeStation(t_AGVPath agvPath1)
        {
            t_Station agvStation1;

            using (var ctx = new AMSContext())
            {
                //read
                List<t_Station> agvStations = ctx.t_Station
                .Where(b => b.AvailableStatus == 1 && b.ChannelNo == agvPath1.Destination && b.OccupiedStatus == 0)
                .OrderBy(b => b.Sequence)
                .ToList();
                //Console.WriteLine(agvWork1);            

                if (agvStations != null && agvStations.Count > 0)
                {
                    agvStation1 = agvStations.First();

                    //对于FIFO，需要考虑AGV是否能进这个站台
                    if (agvStation1.ChannelType == 0 || agvStation1.ChannelType == 1)
                    {
                        List<t_Station> agvStations1 = ctx.t_Station
                        .Where(b => b.Sequence > agvStation1.Sequence && b.OccupiedStatus != 0)
                        .ToList();

                        if (agvStations1 == null || agvStations1.Count > 0)
                        {
                            toolStripStatusLabel1.Text = "找到空的站台 " + agvStation1.StationNo + " ，但是前面站台" + agvStations1.First().StationNo + "有托盘";
                            Console.WriteLine(DateTime.Now.ToString() + "getAvailabeStation 找到空的站台 " + agvStation1.StationNo + " ，但是前面站台" + agvStations1.First().StationNo + "有托盘");
                            return null;
                        }
                    }

                    Console.WriteLine(agvStation1.StationNo);
                    return agvStation1;
                }
                else
                {
                    toolStripStatusLabel1.Text = "通道 " + agvPath1.Destination + " 没有空的站台";
                    return null;
                }
            }

        }

        //查询货位状态
        private t_Station getStationStatus()
        {
            return null;
        }

        //设置通道可存放的物料
        private void setChannelMaterialType(int channelNo, String MaterialType)
        {

        }

        #region button click
        private void button_107_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button_106_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }


        private void button_105_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button_104_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button_103_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button_102_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button_101_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            button_Handler(sender);
        }

        #endregion

        //ProgId: 104
        private void button_Handler(object sender)
        {
            Button button = (Button)sender;

            //检查对应站台是否有任务，如果有，则不允许修改；如果没有，则将有货修改为无货，反之亦然，同时修改站台状态
            using (var ctx = new AMSContext())
            {
                int stationNo = int.Parse(button.Text);
                List<t_AGVWork> agvWorks = ctx.t_AGVWork
                .Where(b => b.Destination == stationNo || b.Origination == stationNo)
                .ToList();

                if (agvWorks != null && agvWorks.Count > 0)
                {
                    Console.WriteLine(DateTime.Now + " button_Handler: 站台：" + button.Text + "有作业任务，无法修改站台状态");
                    toolStripStatusLabel1.Text = DateTime.Now + " button_Handler: 站台：" + button.Text + "有作业任务，无法修改站台状态";
                }
                else
                {
                    t_Station tStation7 = ctx.t_Station
                        .Where(b => b.StationNo == stationNo)
                        .SingleOrDefault();

                    if (button.BackColor == Color.WhiteSmoke)
                    {
                        tStation7.OccupiedStatus = 2;
                        button.BackColor = Color.Blue;

                    }
                    else if (button.BackColor == Color.Blue)
                    {
                        tStation7.OccupiedStatus = 0;
                        button.BackColor = Color.WhiteSmoke;
                    }

                    tStation7.ModifyProgID = 104;
                    tStation7.ModifyTime = DateTime.Now;
                    var setEntry = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(tStation7);
                    setEntry.SetModifiedProperty("OccupiedStatus");
                    setEntry.SetModifiedProperty("ModifyProgID");
                    setEntry.SetModifiedProperty("ModifyTime");
                    ctx.SaveChanges();
                }

            }
        }


        #region 定时任务，定期读取数据库信息，自动更改jobStatus，并更新Station的占用状态OccpiedStatus
        private void timer1_updateJobStatus(object sender)
        {
            t_AGVWork agvWork1 = null;

            if (autoUpdateJobStatus)
            {
                using (var ctx = new AMSContext())
                {
                    var transaction = ctx.Database.BeginTransaction();
                    try
                    {
                        //read
                        List<t_AGVWork> agvWorks = ctx.t_AGVWork
                        //.OrderBy(b => b.BlogId)
                        .Where(b => b.JobStatus == 5 || b.JobStatus == 8 || b.JobStatus == 11 || b.JobStatus == 14 || b.JobStatus == 100)
                        .ToList();
                        //Console.WriteLine(agvWork1);

                        if (agvWorks != null && agvWorks.Count > 0)
                        {
                            //按照任务状态 5,8,11,14更新不同的数值
                            agvWork1 = agvWorks.First();
                            if (agvWork1.JobStatus == 5)
                            {
                                agvWork1.JobStatus = 6;
                            }
                            else if (agvWork1.JobStatus == 8)
                            {
                                agvWork1.JobStatus = 9;
                                //stationRelease = true; //取货完成确认后才允许接收新的搬送请求
                            }
                            else if (agvWork1.JobStatus == 11)
                            {
                                agvWork1.JobStatus = 12;
                            }
                            else if (agvWork1.JobStatus == 14)
                            {
                                agvWork1.JobStatus = 15;
                            }

                            ctx.t_AGVWork.Attach(agvWork1);

                            if (agvWork1.JobStatus == 100) //删除
                            {
                                ctx.t_AGVWork.Remove(agvWork1);
                                ctx.SaveChanges();

                                t_Station tStation5 = ctx.t_Station
                                    .Where(b => b.StationNo == agvWork1.Destination).SingleOrDefault();

                                ctx.t_Station.Attach(tStation5);
                                tStation5.OccupiedStatus = 2;
                                tStation5.ModifyProgID = 201;
                                tStation5.ModifyTime = DateTime.Now;
                                var setEntry = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(tStation5);
                                setEntry.SetModifiedProperty("OccupiedStatus");
                                setEntry.SetModifiedProperty("ModifyProgID");
                                setEntry.SetModifiedProperty("ModifyTime");
                                ctx.SaveChanges();

                                //updateGUIStationStautus(tStation5, 2);
                                toolStripStatusLabel1.Text = "任务从 " + agvWork1.Origination + " 到 " + agvWork1.Destination + " 已经正常完成，站台 " + tStation5.StationNo + " 当前已有托盘";
                            }
                            else
                            {
                                agvWork1.ModifyProgID = 201;

                                var setEntry = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(agvWork1);
                                //只修改实体的Name属性和Age属性
                                setEntry.SetModifiedProperty("JobStatus");
                                setEntry.SetModifiedProperty("ModifyProgID");
                                ctx.SaveChanges();
                            }

                            //Console.WriteLine("update status:", updateFlag);
                            transaction.Commit();

                            String jobStatus = agvWork1.JobStatus.ToString();
                            String jobStatusName = ctx.t_Code
                                .Where(b => b.codetype == jobStatus && b.code == "jobstatus").First().name;
                            toolStripStatusLabel1.Text = "从 " + agvWork1.Origination + " 到 " + agvWork1.Destination + " 的任务 " + jobStatusName;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine(DateTime.Now.ToString() + " timer1_updateJobStatus 更新任务状态失败" + agvWork1.ID);
                        MessageBox.Show(ex.ToString());
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }
            }

            //System.Threading.Thread.Sleep(2000); //用一个线程控制，睡2秒后重新开始
        }

        #endregion

        //定时任务-按照t_AGVWork 表在界面状态栏显示任务执行状态
        private void timer2_updateAGVWorkStatus(object sender)
        {
            if (false) //autoUpdateJobStatus
            {
                using (var ctx = new AMSContext())
                {
                    //read
                    List<t_AGVWork> agvWorks = ctx.t_AGVWork.ToList();
                    if (agvWorks != null && agvWorks.Count > 0)
                    {
                        foreach (t_AGVWork tAGVWork5 in agvWorks)
                        {
                            String jobStatus = tAGVWork5.JobStatus.ToString();
                            String jobStatusName = ctx.t_Code
                                .Where(b => b.codetype == jobStatus && b.code == "jobstatus").First().name;
                            toolStripStatusLabel1.Text = "从 " + tAGVWork5.Origination + " 到 " + tAGVWork5.Destination + " 的任务 " + jobStatusName;
                        }
                    }
                }
            }
        }

        //定时任务-按照t_Station 表状态更新界面颜色
        private void timer3_GUIStationStautus(object sender)
        {
            updateGUIStationStautus(groupBox1);
            updateGUIStationStautus(groupBox2);
        }

        private void button_opc_connection_test_Click(object sender, EventArgs e)
        {
            try
            {
                String value = opcUaClient.ReadNode<string>("ns=2;s=AHC.Conveyor.test");
                MessageBox.Show(value); // 显示测试数据
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + "无法连接PLC");
            }
        }

        private void Timer_CheckedChanged(object sender, EventArgs e)
        {
            if (checkbox_Autoupdate.Checked)
            {
                autoUpdateJobStatus = true;
            }
            else
            {
                autoUpdateJobStatus = false;
            }
        }


        private void stationRelease_CheckedChanged(object sender, EventArgs e)
        {
            if (checkbox_stationRelease.Checked)
            {
                stationRelease = true;
            }
            else
            {
                stationRelease = false;
            }
        }

        private void checkBox_connectedWithOPC_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_connectedWithOPC.Checked)
            {
                connectedWithOPC = true;
            }
            else
            {
                connectedWithOPC = false;
            }
        }

        //定时任务-显示当前是否可以接收conveyor的搬送请求，调试时可以禁用;
        //当存在启动是当前站台的任务，并且任务状态小于9的，就不允许接收
        private void enable_opc_read(object sender)
        {
            using (var ctx = new AMSContext())
            {
                //read
                List<t_AGVWork> agvWorks = ctx.t_AGVWork
                    .Where(b => b.Origination == 301 && b.JobStatus < 9)
                    .ToList();
                if (agvWorks != null && agvWorks.Count > 0)
                {
                    checkbox_stationRelease.Checked = false;
                    checkbox_stationRelease.Enabled = false;
                    stationRelease = false;
                }
                else
                {
                    checkbox_stationRelease.Checked = true;
                    checkbox_stationRelease.Enabled = true;
                    stationRelease = true;
                }
            }
        }


        //定时任务-读取opc服务器的信号后，尝试创建任务
        private void timer_opc_read(object sender)
        {
            if (connectedWithOPC)
            {
                try
                {
                    Boolean palletA = opcUaClient.ReadNode<bool>("ns=2;s=AHC.Conveyor.PalletA");
                    Boolean palletB = opcUaClient.ReadNode<bool>("ns=2;s=AHC.Conveyor.PalletB");
                    //MessageBox.Show(palletA.ToString()); // 显示测试数据
                    if (palletA && palletB)
                    {
                        MessageBox.Show("未提供正确的物料类型，请联系输送机厂商");
                    }
                    else
                    {
                        if (palletA)
                        {
                            abc(1);
                        }
                        else if (palletB)
                        {
                            abc(2);
                        }
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show("初始化出错：" + err.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void abc(int materialType)
        {
            int stationNo = 301;

            if (stationRelease)
            {
                triggerTask(materialType, stationNo);
            }
            else
            {
                Console.WriteLine(DateTime.Now + " PLC signal received and ignore, because station is not ready for pick-up");
                toolStripStatusLabel1.Text = DateTime.Now + " PLC signal received and ignore, because station is not ready for pick-up";
            }

        }

        //复位站台状态
        private void Btn_Reset_Channel_A_Click(object sender, EventArgs e)
        {
            Reset_Channel(10000);
        }

        //复位站台状态
        private void Btn_Reset_Channel_B_Click(object sender, EventArgs e)
        {
            Reset_Channel(20000);
        }

        //ProgID 103
        private Boolean Reset_Channel(int channelNo)
        {
            String channelName;
            if (channelNo == 10000)
            {
                channelName = "A";
            }
            else
            {
                channelName = "B";
            }
            //reset前，检查针对这个channel的执行中的任务，如果有，Messagebox报错，不能reset，请先删除指令
            using (var ctx = new AMSContext())
            {
                List<t_Station> agvStations = ctx.t_Station
                .Where(b => b.AvailableStatus == 1 && b.ChannelNo == channelNo)
                .OrderBy(b => b.Sequence)
                .ToList();
                //Console.WriteLine(agvWork1);            

                if (agvStations != null && agvStations.Count > 0)
                {
                    var transaction = ctx.Database.BeginTransaction(); //事务开始
                    try
                    {
                        foreach (t_Station tStation6 in agvStations)
                        {
                            List<t_AGVWork> agvWorks1 = ctx.t_AGVWork
                                .Where(b => b.Destination == tStation6.StationNo)
                                .ToList();

                            if (agvWorks1.Count > 0)
                            {
                                toolStripStatusLabel1.Text = "通道 " + channelName + " 无法重置，搬送ID： " + agvWorks1.First().ID + "正在驶往站台 " + agvWorks1.First().Destination;
                                Console.WriteLine(DateTime.Now.ToString() + "Reset_Channel 通道 " + channelName + " 无法重置，搬送ID： " + agvWorks1.First().ID + "正在驶往站台 " + agvWorks1.First().Destination);
                                throw new Exception("站台有任务，无法重置");
                            }
                            else
                            {
                                tStation6.OccupiedStatus = 0;
                                tStation6.ModifyProgID = 103;
                                tStation6.ModifyTime = DateTime.Now;
                                var setEntry = ((IObjectContextAdapter)ctx).ObjectContext.ObjectStateManager.GetObjectStateEntry(tStation6);
                                setEntry.SetModifiedProperty("OccupiedStatus");
                                setEntry.SetModifiedProperty("ModifyProgID");
                                setEntry.SetModifiedProperty("ModifyTime");
                                ctx.SaveChanges();
                            }
                        }

                        transaction.Commit();

                        Console.WriteLine(DateTime.Now.ToString() + " Reset_Channel：通道" + channelName + " 已重置");
                        toolStripStatusLabel1.Text = DateTime.Now.ToString() + " Reset_Channel：通道" + channelName + " 已重置";
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        Console.WriteLine(DateTime.Now.ToString() + " Reset_Channel：失败了，事务回滚");
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }
                else
                {
                    toolStripStatusLabel1.Text = "通道 " + channelNo + " 内没有站台，请确认通道号是否正确";
                    Console.WriteLine(DateTime.Now.ToString() + "Reset_Channel 通道 " + channelNo + " 内没有站台，请确认通道号是否正确");
                    return false;
                }

                return true;
            }
        }

    }
}
