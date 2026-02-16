-- /home/coco/src/forth_server/search.lua
local luasql = require "luasql.mysql"
local mode, val = arg[1], arg[2]
if not val or val == "" then return end

local env = assert(luasql.mysql())
local con = assert(env:connect("zipcode_db", "cocozip", "BB4-QerHVORyNIzr", "127.0.0.1"))

local sql
if mode == "ZIP" then
    sql = string.format("SELECT * FROM zipcode WHERE zipcode LIKE '%s%%' LIMIT 50", val:gsub("[^%d]", ""))
else
    sql = string.format("SELECT * FROM zipcode WHERE prefectures LIKE '%%%s%%' OR city LIKE '%%%s%%' OR town LIKE '%%%s%%' LIMIT 50", val, val, val)
end

local cur = assert(con:execute(sql))
local row = cur:fetch({}, "a")

while row do
    local full_addr = (row.prefectures or "").. (row.city or "").. (row.town or "")
    local fmt_zip = (row.zipcode or ""):sub(1,3) .. "-" .. (row.zipcode or ""):sub(4)
    
    local tr = string.format([[
    <tr>
        <td><span style="font-family:'Courier New', monospace; font-weight:bold; color:#3776ab; font-size:1.1rem;">%s</span></td>
        <td><span style="font-size:1.1rem; font-weight:500;">%s</span></td>
        <td>
            <div class="btn-box">
                <button onclick="openMap('%s')" class="action-btn maps-btn">MAP</button>
                <button onclick="openAI('%s')" class="action-btn ai-btn">AI</button>
            </div>
        </td>
    </tr>]], fmt_zip, full_addr, full_addr, full_addr)
    
    print(tr)
    row = cur:fetch(row, "a")
end

cur:close() con:close() env:close()
