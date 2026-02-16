\ /home/coco/src/forth_server/search.fs
require string.fs

: do-search
  s" ADDR_VAL" getenv dup 0 > if
    \ 住所検索
    s" lua5.3 /home/coco/src/forth_server/search.lua ADDR '" 2swap s+ s" '" s+ system
  else
    2drop s" ZIP_VAL" getenv dup 0 > if
      \ 郵便番号検索
      s" lua5.3 /home/coco/src/forth_server/search.lua ZIP '" 2swap s+ s" '" s+ system
    else
      2drop
    then
  then
  \ Forthレイヤーのサイン（デバッグ用）
  ." "
;

do-search bye
