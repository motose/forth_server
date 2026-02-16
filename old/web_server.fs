\ web_server.fs
require unix/socket.fs

8122 constant PORT

: hello-server
    \ 1. TCPソケット作成 (PF_INET=2, SOCK_STREAM=1)
    2 1 0 socket ( sock-fd )
    dup 0 < if ." Socket Error" cr bye then
    
    \ 2. ポート再利用の設定 (SO_REUSEADDR)
    dup >r 1 { w^ opt } 
    r@ 1 2 opt 4 setsockopt drop ( 1=SOL_SOCKET, 2=SO_REUSEADDR )

    \ 3. バインド (INADDR_ANY=0)
    r@ 0 PORT bind-new-socket ( r: sock-fd )
    dup 0 < if ." Bind Error: Port " PORT . cr bye then drop

    \ 4. リッスン
    r@ 5 listen drop
    
    cr ." Server started on http://127.0.0.1:8122/" cr
    
    begin
        \ 5. 接続受け入れ
        r@ 0 0 accept ( client-fd )
        dup 0 > if
            >r
            \ 6. HTTPレスポンス送信
            s" HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: 22\r\n\r\n<h1>Hello, World</h1>" 
            r@ write-socket drop
            
            \ 7. 閉じる
            r> close-socket drop
            ." ." \ 接続のたびにドットを表示
        else
            drop
        then
    again
    r> close-socket drop ;

hello-server
