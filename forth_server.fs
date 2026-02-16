\ /home/coco/src/forth_server/forth_server.fs
s" /home/coco/src/forth_server/libforth_bridge.so" open-lib constant lib
s" start_web" lib lib-sym constant _start_web
s" web_clear" lib lib-sym constant _web_clear
s" web_push"  lib lib-sym constant _web_push
s" web_input_char" lib lib-sym constant _web_in
s" request_ready"  lib lib-sym constant _ready
s" db_result_out"  lib lib-sym constant _db_out
s" db_clear"  lib lib-sym constant _db_clear
s" db_push"   lib lib-sym constant _db_push
s" db_exec"   lib lib-sym constant _db_exec
s" request_param" lib lib-sym constant _param_ptr

: z-count ( addr -- addr len ) dup 0 begin over over + c@ 0<> while 1+ repeat nip ;
: >web ( addr len -- ) 0 ?do dup i + c@ _web_in ! 0 _web_push call-c drop loop drop ;
: z>web ( addr -- ) begin dup c@ 0<> while dup c@ _web_in ! 0 _web_push call-c drop 1+ repeat drop ;
: >db ( addr len -- ) 0 ?do dup i + c@ _web_in ! 0 _db_push call-c drop loop drop ;

: draw-page
    0 _web_clear call-c drop
    s" <!DOCTYPE html><html><head><meta charset='utf-8'><title>Forth DB サービス >web
    s" <link rel='icon' href='https://forth-standard.org/images/forth.png' type='image/png'>" >web
    s" <style>body{margin:0;font-family:sans-serif;background-color:#f0f2f5;}" >web
    s" .header{background-color:#1a1a1a;color:white;padding:0 20px;display:flex;align-items:center;justify-content:space-between;height:65px;}" >web
    s" .container{padding:40px;max-width:1000px;margin:0 auto;}" >web
    s" .search-row{display:flex;gap:20px;margin-bottom:20px;}.card{background:white;padding:25px;border-radius:8px;box-shadow:0 4px 6px rgba(0,0,0,0.1);flex:1;}" >web
    s" .btn-box{display:flex;gap:8px;justify-content:flex-end;}" >web
    s" .action-btn{display:inline-flex;align-items:center;gap:6px;padding:8px 14px;border-radius:6px;text-decoration:none;font-size:13px;font-weight:bold;cursor:pointer;color:white;border:none;}" >web
    s" .maps-btn{background-color:#ea4335;}.ai-btn{background-color:#10a37f;}input[type='text']{width:100%;box-sizing:border-box;padding:12px;border:1px solid #ddd;border-radius:4px;}" >web
    s" button.search-btn{width:100%;padding:12px;background-color:#007d9c;color:white;border:none;border-radius:4px;cursor:pointer;font-weight:bold;margin-top:10px;}" >web
    s" table{width:100%;border-collapse:collapse;margin-top:20px;}td{border-bottom:1px solid #eee;padding:12px;vertical-align:middle;}" >web
    s" .nav-btn{display:inline-flex;align-items:center;padding:6px 16px;margin-right:12px;border-radius:20px;text-decoration:none;font-size:13px;font-weight:bold;color:white;border:1px solid rgba(255,255,255,0.4);background-color:rgba(255,255,255,0.1);transition:all 0.2s ease;}" >web
    s" .nav-btn:hover{background-color:white;color:#007d9c;transform:translateY(-1px);}</style>" >web
    s" <script>function openMap(a){window.open('https://www.google.com/maps/search/'+encodeURIComponent(a),'_blank');}" >web
    s" function openAI(a){window.open('https://chatgpt.com/?q='+encodeURIComponent(a+' について詳しく教えて'),'_blank');}</script></head><body>" >web
    s" <div class='header'><div style='display:flex;align-items:center;'>" >web
    s" <img src='https://forth-standard.org/images/forth.png' style='height:40px;margin-right:15px;'>" >web
    s" <span style='font-size:2rem;font-weight:bold;'>Forth DB サービス</span></div>" >web
    s" <div style='display:flex;align-items:center;'>" >web
    s" <a href='https://ja.wikipedia.org/wiki/Forth' target='_blank' class='nav-btn'>Info</a>" >web
    s" <a href='/docs/readme.html' class='nav-btn'>ReadMe</a>" >web
    s" <img src='https://sv1.etech21.net/assets/eTech21.png' style='height:35px;margin-right:10px;'><span style='font-size:1.5rem;color:#ccc;'>e-Tech21.net</span>" >web
    s" </div></div><div class='container'><div class='search-row'>" >web
    s" <div class='card'><h2>住所検索</h2><form action='/' method='GET'><input type='text' name='addr' placeholder='新宿区歌舞伎町'>" >web
    s" <button type='submit' name='addr-btn' class='search-btn'>住所から調べる</button></form></div>" >web
    s" <div class='card'><h2>郵便番号検索</h2><form action='/' method='GET'><input type='text' name='zip' placeholder='1600021' maxlength='7'>" >web
    s" <button type='submit' name='zip-btn' class='search-btn'>番号から調べる</button></form></div></div>" >web
    s" <div class='card'><h2>検索結果</h2><table>" >web _db_out z>web s" </table></div></div></body></html>" >web ;

: main
    page ." [Forth DB サービス] 起動..." cr
    \ 初期メッセージセット
    s" <tr><td colspan='3'>検索条件を入力してください。</td></tr>" _db_out swap move
    draw-page
    0 _start_web call-c drop

    ." [System] サービスとして常駐します。" cr

    begin
        _ready l@ 1 = if
            0 _db_clear call-c drop
            s" SELECT zipcode, prefectures, city, town FROM zipcode WHERE " >db
            _param_ptr z-count s" ADDR:" search if 5 /string dup 0> if
                s" CONCAT(prefectures, city, town) LIKE '%" >db 2dup >db s" %' " >db 
            else 2drop s" 1=0 " >db then else 2drop
            _param_ptr z-count s" ZIP:" search if 4 /string dup 0> if
                s" zipcode LIKE '" >db 2dup >db s" %' " >db
            else 2drop s" 1=0 " >db then else 2drop s" 1=0 " >db then then
            s"  LIMIT 50" >db 0 _db_exec call-c drop
            draw-page 0 _ready l!
        then
        100 ms \ CPU負荷を下げるためのウェイト
    again ; \ キー入力を待たず、永久にループする

main
