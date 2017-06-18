using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace StockBot.DlgControl
{
    class StockChart : SingleTon<StockChart>
    {
        private Chart chartStock_;
        private Chart chartMacd_;
        private int limitXCount_ = 150;

        private StockData stockData_;
        private PRICE_TYPE priceType_;
        private Font xlabelFont_;

        StockChart()
        {
            StockBotDlg stockBotDlg = Program.stockBotDlg_;
            xlabelFont_ = getFont(10.0F, FontStyle.Regular);

            chartStock_ = stockBotDlg.chart_Stock();
            chartStock_.ChartAreas[0].AxisX.LabelStyle.Font = xlabelFont_;
            chartStock_.Titles.Add("stockTitle");
            chartStock_.GetToolTipText += this.chartStockToolTipText;

            chartMacd_ = stockBotDlg.chart_Macd();
            chartMacd_.ChartAreas[0].AxisX.LabelStyle.Font = xlabelFont_;
            chartMacd_.Titles.Add("stockTitle");

            stockData_ = null;
            this.setUnit(PRICE_TYPE.DAY);
        }

        private Font getFont(float fontSize, FontStyle style)
        {
            Font font = new Font("font", fontSize, style);
            return font;
        }

        private void chartStockToolTipText(object sender, ToolTipEventArgs e)
        {
            // Check selected chart element and set tooltip text for it
            switch (e.HitTestResult.ChartElementType) {
                case ChartElementType.DataPoint:
                    var dataPoint = e.HitTestResult.Series.Points[e.HitTestResult.PointIndex];
                    e.Text = string.Format("X:\t{0}\nY:\t{1}", dataPoint.XValue, dataPoint.YValues[0]);
                    break;
            }
        }

        public void setUnit(PRICE_TYPE type)
        {
            priceType_ = type;
            if (priceType_ == PRICE_TYPE.DAY) {
                limitXCount_ = 50;
            }
            else {
                limitXCount_ = 150;
            }
            this.redrawStock();     //다시 그리기
        }

        private double minPrice_ = 0, maxPrice_ = 0;
        private double minMacd_ = 0, maxMacd_ = 0;

        private void redrawStock()
        {
            this.drawStock(stockData_);     //다시 그리기
        }

        public void drawStock(StockData stockData)
        {
            if (stockData == null) {
                return;
            }
            if (stockData.priceTable(priceType_) == null) {
                return;
            }

            if (stockData_ != null) {
                stockData_ = null;
            }
            stockData_ = (StockData)stockData.Clone();

            if (stockData_.priceTable(priceType_).Count <= 120) {
                return;
            }

            this.drawStockGraph();
        }

        public void removeStockGraph()
        {
            if (stockData_ != null) {
                stockData_ = null;
            }
            chartStock_.Series.Clear();
            chartMacd_.Series.Clear();
        }

        private void drawStockGraph()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            if (priceTable == null) {
                return;
            }
            {
                minPrice_ = 0; maxPrice_ = 0;

                chartStock_.Series.Clear();
                this.setTitle();
                this.drawPrice();
                //  this.drawSimpleAvg();
                this.drawExpAvg();
                this.drawBollinger();

                chartStock_.ChartAreas[0].AxisX.LabelStyle.Interval = 30;
                chartStock_.ChartAreas[0].AxisY.Minimum = minPrice_;
                chartStock_.ChartAreas[0].AxisY.Maximum = maxPrice_;
                chartStock_.ChartAreas[0].AxisY.LabelStyle.Format = "#.#";
            }
            {
                minMacd_ = 0; maxMacd_ = 0;
                chartMacd_.Series.Clear();
                this.drawMacd();

                chartMacd_.ChartAreas[0].AxisY.Minimum = minMacd_;
                chartMacd_.ChartAreas[0].AxisY.Maximum = maxMacd_;
                chartMacd_.ChartAreas[0].AxisY.LabelStyle.Format = "#.#";
            }
        }

        private Font titleFont_ = null;
        private void setTitle()
        {
            string title = stockData_.codeString() + ":" + stockData_.name_ + " / " + priceType_.ToString();

            if (titleFont_ != null) {
                titleFont_ = null;
            }
            titleFont_ = this.getFont(14.0F, FontStyle.Bold);
            chartStock_.Titles[0].Font = titleFont_;
            chartStock_.Titles[0].Text = title;
            chartStock_.ChartAreas[0].BorderDashStyle = ChartDashStyle.Solid;    /* Border 영역 줄 긋기 */

            chartMacd_.Titles[0].Font = titleFont_;
            chartMacd_.Titles[0].Text = title;
            chartMacd_.ChartAreas[0].BorderDashStyle = ChartDashStyle.Solid;    /* Border 영역 줄 긋기 */
        }

        private void drawPrice()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            int xMax = priceTable.Count - 121;  //120 평균이라 하위 120개는 빼줘야 함
            if (xMax < 0) {
                return;
            }
            xMax = Math.Min(limitXCount_, priceTable.Count);

            Series chart = chartStock_.Series.Add("가격선");

            chart.ChartType = SeriesChartType.Candlestick;
            chart["PriceUpColor"] = "Red";
            chart["PriceDownColor"] = "Blue";
            chart.BorderWidth = 1;

            Dictionary<string, int[]> chartDataPool = new Dictionary<string, int[]>();

            int index = 0;
            for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                PriceData priceData = priceTable[dateIdx];
                // 캔들 주식 값의 y축 데이터는 고가, 저가, 시가, 종가 순임
                int[] price = { priceData.highPrice_, priceData.lowPrice_, priceData.startPrice_, priceData.price_ };

                chartDataPool.Add(priceData.date_, price);

                if (minPrice_ == 0) {
                    minPrice_ = priceData.lowPrice_;
                    maxPrice_ = priceData.highPrice_;
                }
                minPrice_ = Math.Min(minPrice_, priceData.lowPrice_);
                maxPrice_ = Math.Max(maxPrice_, priceData.highPrice_);

                index++;
            }

            foreach (KeyValuePair<string, int[]> chartData in chartDataPool) {
                int i = 0;
                chart.Points.AddXY(chartData.Key, chartData.Value[i++], chartData.Value[i++], chartData.Value[i++], chartData.Value[i++]);
            }
        }

        private void drawSimpleAvg()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            int xMax = priceTable.Count - 121;  //120 평균이라 하위 120개는 빼줘야 함
            if (xMax < 0) {
                return;
            }
            xMax = Math.Min(limitXCount_, priceTable.Count);

            for (AVG_SAMPLEING avgIdx = AVG_SAMPLEING.AVG_5; avgIdx < AVG_SAMPLEING.AVG_MAX; ++avgIdx) {
                Series chart = chartStock_.Series.Add("단순 " + avgIdx.ToString());
                chart.ChartType = SeriesChartType.Line;
                chart.BorderDashStyle = ChartDashStyle.Dot;

                Dictionary<string, double> chartDataPool = new Dictionary<string, double>();

                int index = 0;
                for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                    double avg = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.SMA_START + (int)avgIdx];
                    chartDataPool.Add(priceTable[dateIdx].date_, avg);

                    if (minPrice_ == 0) {
                        minPrice_ = maxPrice_ = avg;
                    }
                    minPrice_ = Math.Min(minPrice_, avg);
                    maxPrice_ = Math.Max(maxPrice_, avg);

                    index++;
                }

                foreach (KeyValuePair<string, double> chartData in chartDataPool) {
                    chart.Points.AddXY(chartData.Key, chartData.Value);
                }
            }
        }

        private void drawExpAvg()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            int xMax = priceTable.Count - 121;  //120 평균이라 하위 120개는 빼줘야 함
            if (xMax < 0) {
                return;
            }
            xMax = Math.Min(limitXCount_, priceTable.Count);

            for (AVG_SAMPLEING avgIdx = AVG_SAMPLEING.AVG_5; avgIdx < AVG_SAMPLEING.AVG_MAX; ++avgIdx) {
                Series chart = chartStock_.Series.Add("지수 " + avgIdx.ToString());
                chart.ChartType = SeriesChartType.Line;
                chart.BorderDashStyle = ChartDashStyle.Dot;

                Dictionary<string, double> chartDataPool = new Dictionary<string, double>();

                int index = 0;
                for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                    double avg = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.EMA_START + (int)avgIdx];
                    chartDataPool.Add(priceTable[dateIdx].date_, avg);

                    if (minPrice_ == 0) {
                        minPrice_ = maxPrice_ = avg;
                    }
                    minPrice_ = Math.Min(minPrice_, avg);
                    maxPrice_ = Math.Max(maxPrice_, avg);

                    index++;
                }

                foreach (KeyValuePair<string, double> chartData in chartDataPool) {
                    chart.Points.AddXY(chartData.Key, chartData.Value);
                }
            }
        }

        delegate void drawingBollinger(bool upper);
        private void drawBollinger()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            int xMax = priceTable.Count - 121;  //120 평균이라 하위 120개는 빼줘야 함
            if (xMax < 0) {
                return;
            }
            xMax = Math.Min(limitXCount_, priceTable.Count);

            drawingBollinger drawing = (upper) => {
                Series chart;
                if (upper) {
                    chart = chartStock_.Series.Add("볼린져 상한");
                }
                else {
                    chart = chartStock_.Series.Add("볼린져 하한");
                }
                chart.ChartType = SeriesChartType.Line;
                chart.BorderWidth = 2;
                chart.BorderDashStyle = ChartDashStyle.Dash;

                Dictionary<string, double> chartDataPool = new Dictionary<string, double>();

                int index = 0;
                for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                    double price = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.BOLLINGER_LOWER];
                    if (upper) {
                        price = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.BOLLINGER_UPPER];
                    }

                    chartDataPool.Add(priceTable[dateIdx].date_, price);

                    if (minPrice_ == 0) {
                        minPrice_ = maxPrice_ = price;
                    }
                    minPrice_ = Math.Min(minPrice_, price);
                    maxPrice_ = Math.Max(maxPrice_, price);

                    index++;
                }

                foreach (KeyValuePair<string, double> chartData in chartDataPool) {
                    chart.Points.AddXY(chartData.Key, chartData.Value);
                }
            };

            drawing(true);      // 볼린저 upper_
            drawing(false);     // 볼린저 lower_
        }

        enum MACD_TYPE
        {
            MACD,
            SIGNAL,
            OSCIL,
        }
        delegate void drawingMACD(MACD_TYPE macdType);
        private void drawMacd()
        {
            List<PriceData> priceTable = stockData_.priceTable(priceType_);
            int xMax = priceTable.Count - 121;  //120 평균이라 하위 120개는 빼줘야 함
            if (xMax < 0) {
                return;
            }
            xMax = Math.Min(limitXCount_, priceTable.Count);

            maxMacd_ = minMacd_ = priceTable[xMax].calc_[(int)EVALUATION_DATA.MACD];

            drawingMACD drawing = (macdType) => {
                Series chart = chartMacd_.Series.Add(macdType.ToString());
                switch (macdType) {
                    case MACD_TYPE.MACD:
                    case MACD_TYPE.SIGNAL:
                        chart.ChartType = SeriesChartType.Line;
                        chart.BorderWidth = 1;
                        chart.BorderDashStyle = ChartDashStyle.Dash;
                        break;
                    default:
                        return;
                }

                Dictionary<string, double> chartDataPool = new Dictionary<string, double>();

                int index = 0;
                for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                    double price = 0.0f;
                    switch (macdType) {
                        case MACD_TYPE.MACD: price = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.MACD]; break;
                        case MACD_TYPE.SIGNAL: price = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.MACD_SIGNAL]; break;
                        default:
                            return;
                    }

                    chartDataPool.Add(priceTable[dateIdx].date_, price);

                    minMacd_ = Math.Min(minMacd_, price);
                    maxMacd_ = Math.Max(maxMacd_, price);
                    index++;
                }

                foreach (KeyValuePair<string, double> chartData in chartDataPool) {
                    chart.Points.AddXY(chartData.Key, chartData.Value);
                }
            };

            drawingMACD oscilDraw = (macdType) => {
                Series chart = chartMacd_.Series.Add(macdType.ToString());
                chart.ChartType = SeriesChartType.Stock;
                chart.BorderWidth = 2;

                Dictionary<string, double> chartDataPool = new Dictionary<string, double>();

                int index = 0;
                for (int dateIdx = xMax; dateIdx >= 0; --dateIdx) {
                    double price = priceTable[dateIdx].calc_[(int)EVALUATION_DATA.MACD_OSCIL];

                    chartDataPool.Add(priceTable[dateIdx].date_, price);

                    minMacd_ = Math.Min(minMacd_, price);
                    maxMacd_ = Math.Max(maxMacd_, price);
                    index++;
                }

                index = 0;
                double oldValue = 0.0f;
                foreach (KeyValuePair<string, double> chartData in chartDataPool) {
                    int i = 0;
                    double[] value = { chartData.Value, 0, 0, chartData.Value };
                    chart.Points.AddXY(chartData.Key, value[i++], value[i++], value[i++], value[i++]);
                    if (value[0] <= 0) {
                        chart.Points[index].Color = Color.Blue;
                    }
                    else {
                        chart.Points[index].Color = Color.Red;
                    }
                    oldValue = chartData.Value;
                    index++;
                }
            };

            drawing(MACD_TYPE.MACD);
            drawing(MACD_TYPE.SIGNAL);
            oscilDraw(MACD_TYPE.OSCIL);
        }
    }
}
