using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MGT
{
    public partial class Form1 : Form
    {
        t_Station agvStation1;
        t_AGVWork2 agvWork2;
        t_AGVPath agvPath1;
        List<t_Station> stationList = new List<t_Station>();

        //const
        int jobType_1 = 1;
        int jobType_2 = 2;

        public Form1()
        {
            InitializeComponent();
        }

        private void CallA_Click(object sender, EventArgs e)
        {
            //t_Station station2 = new t_Station();
            using (var ctx = new AMSContext())
            {
                //read
                agvStation1 = ctx.t_Station
                    //.OrderBy(b => b.BlogId)
                    .Where(b => b.StationNo == 301)
                    .First();
                Console.WriteLine(agvStation1.StationNo);

                /*
                  var station2 = from s in ctx.t_Station
                                  where s.StationNo == 301
                                  select s;
                                */
                //ctx.t_Station.SqlQuery("select * from t_Station");
            }
            triggerTask(1, agvStation1);
        }

        private void CallB_Click(object sender, EventArgs e)
        {
        }

        //修改界面每个货位的颜色，空为灰色，有货为蓝色
        private void changeOccupiedStatus(t_Station station3)
        {

        }

        //触发任务
        private void triggerTask(int callButtonSignal,t_Station originalStation)
        {
            //从站台获取call信号A 或 B
            //判断是否有任务整在前往源站台，如果有就略过

            //根据规则找到找路径，一般只能找到一条，如果找到多条，按照规则选一条
            agvPath1 = getPath(originalStation, callButtonSignal);

            //如果路径下有多个站台，按照规格选一个站台
            agvStation1 = getAvailabeStation(agvPath1);

            //构建任务实体
            agvWork2 = new t_AGVWork2
            { 
                Origination = originalStation.StationNo,
                Destination = agvStation1.StationNo,
            };

            //创建任务
            createTask(agvWork2);
        }

        //创建任务
        private void createTask(t_AGVWork2 agvWork2)
        {
            using (var ctx = new AMSContext())
            {
                ctx.t_AGVWork2.Add(agvWork2);
                ctx.SaveChanges();
            }
        }


        //更新任务
        private void updateTask(t_AGVWork agvWork3)
        {
        }

        //计算可用路径-业务相关-根据起点找到所有可用路径
        private t_AGVPath getPath(t_Station originalStation, int callButtonSignal)
        {
            using (var ctx = new AMSContext())
            {

                //read
                agvPath1 = ctx.t_AGVPath
                    //.OrderBy(b => b.BlogId)
                    .Where(b => b.AvailableStatus == 1 && b.Origination == originalStation.StationNo) 
                    .First();
                Console.WriteLine(agvPath1.Destination);

                /*
                var stationList = from s in ctx.t_AGVPath
                                  where s.AvailableStatus == 1 && s.Origination == originalStation.StationNo
                                  select s;
                */
                //ctx.t_Station.SqlQuery("select * from t_Station");
            }
            return agvPath1;
        }


        //计算可用货位
        private t_Station getAvailabeStation(t_AGVPath agvPath1)
        {
            using (var ctx = new AMSContext())
            {
                //read
                agvStation1 = ctx.t_Station
                .Where(b => b.AvailableStatus == 1 && b.ChannelNo == agvPath1.Destination)
                .OrderBy(b => b.Sequence)
                .First();
                Console.WriteLine(agvStation1.StationNo);

                /*
                var stationList = from station1 in stationList3
                              where station1.OccupiedStatus == 0
                              orderby station1.Sequence ascending
                              select station1;
                              */
            }
            return agvStation1;
        }

        //查询货位状态
        private t_Station getStationStatus()
        {
            return agvStation1; 
        }      

        //更改货位状态（reset）
        private void updateStationStatus(t_Station destStation)
        {

        }

        //设置通道可存放的物料
        private void setChannelMaterialType(int channelNo, String MaterialType)
        {

        }

        private void button_107_Click(object sender, EventArgs e)
        {
            button_107.BackColor = Color.Blue;
        }



        //站台状态是否允许改变

        //更新界面颜色

    }
}
