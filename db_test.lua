-- /home/coco/src/forth_server/db_test.lua
local driver = require "luasql.mysql"
local env = assert(driver.mysql())

-- DB接続設定
local con = assert(env:connect("zipcode_db", "cocozip", "BB4-QerHVORyNIzr", "localhost"))

local function fetch_zipcodes(limit)
    print(string.format("\n--- Fetching %d record(s) ---", limit))
    local cur = assert(con:execute(string.format("SELECT zipcode, prefectures, city, town FROM zipcode LIMIT %d", limit)))
    
    local row = cur:fetch({}, "a")
    while row do
        print(string.format("Zip: %s | %s%s%s", row.zipcode, row.prefectures, row.city, row.town))
        row = cur:fetch(row, "a")
    end
    
    cur:close()
end

-- 1. まず1レコード取得
fetch_zipcodes(1)

-- 2. 次に10レコード取得
fetch_zipcodes(10)

con:close()
env:close()
