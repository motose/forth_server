## Forth DB サービス (forth_server)

Forthを使用してDB連携のサーバーサイドWEBアプリケーションが実現可能かの実験サイト。
住所から郵便番号の検索、郵便番号から住所の検索の機能を有し、結果として得た住所のマップ（Google Maps）とAI（ChatGPT）による詳細情報の表示が行える。

### DB連携
MySQL C APIをFORTHのC FFIで使う。

### WEB連携
MHD microhttpdをFORTのC FFIで使う。

### ビルド
gcc -shared -o libforth_bridge.so forth_bridge.c $(mysql_config --cflags --libs) -lmicrohttpd -fPIC

### 公開
本サービスはhttp://127.0.0.1:8122/で稼働しているが、OpenRestyでSSL化してサブドメインにリバースプロキシで接続しhttps://forth.etech21.net/として公開している。
systemdサービスにより常駐化している。




