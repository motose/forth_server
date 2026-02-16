/* /home/coco/src/forth_server/forth_bridge.c */
#include <microhttpd.h>
#include <mysql/mysql.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>

static char web_buffer[131072]; // バッファサイズを拡大
static int web_cursor = 0;
char request_param[1024];
int request_ready = 0;
int web_input_char = 0;

static char sql_buffer[1024];
static int sql_cursor = 0;
char db_result_out[98304]; 

void web_clear() { web_cursor = 0; memset(web_buffer, 0, sizeof(web_buffer)); }
void web_push()  { if(web_cursor < 131071) web_buffer[web_cursor++] = (char)web_input_char; }

void db_clear()  { sql_cursor = 0; memset(sql_buffer, 0, sizeof(sql_buffer)); }
void db_push()   { if(sql_cursor < 1023) sql_buffer[sql_cursor++] = (char)web_input_char; }

void db_exec() {
    MYSQL *conn = mysql_init(NULL);
    mysql_real_connect(conn, "localhost", "cocozip", "BB4-QerHVORyNIzr", "zipcode_db", 3306, NULL, 0);
    mysql_set_character_set(conn, "utf8mb4");
    mysql_query(conn, sql_buffer);
    MYSQL_RES *res = mysql_store_result(conn);
    
    db_result_out[0] = '\0';
    MYSQL_ROW row;
    while ((row = mysql_fetch_row(res))) {
        char line[1024];
        // Node.js版と同等のボタン・スクリプトを含む行を生成
        snprintf(line, sizeof(line), 
            "<tr><td><span style='font-family:monospace; font-weight:bold; color:#5e5086;'>〒%.3s-%.4s</span></td>"
            "<td><span style='font-size:1.1rem;'>%s%s%s</span></td>"
            "<td><div class='btn-box'>"
            "<button onclick=\"openMap('%s%s%s')\" class='action-btn maps-btn'>MAP</button>"
            "<button onclick=\"openAI('%s%s%s')\" class='action-btn ai-btn'>AI</button>"
            "</div></td></tr>",
            row[0], row[0]+3, row[1], row[2], row[3],
            row[1], row[2], row[3], row[1], row[2], row[3]);
        strncat(db_result_out, line, sizeof(db_result_out) - strlen(db_result_out) - 1);
    }
    if (strlen(db_result_out) == 0) strcpy(db_result_out, "<tr><td colspan='3'>該当なし</td></tr>");
    mysql_free_result(res);
    mysql_close(conn);
}

static enum MHD_Result handler(void *cls, struct MHD_Connection *c, const char *url, const char *m, const char *v, const char *d, size_t *s, void **con) {
    const char *a = MHD_lookup_connection_value(c, MHD_GET_ARGUMENT_KIND, "addr");
    const char *z = MHD_lookup_connection_value(c, MHD_GET_ARGUMENT_KIND, "zip");
    const char *ab = MHD_lookup_connection_value(c, MHD_GET_ARGUMENT_KIND, "addr-btn");
    const char *zb = MHD_lookup_connection_value(c, MHD_GET_ARGUMENT_KIND, "zip-btn");

    if (ab && a) { snprintf(request_param, 1023, "ADDR:%s", a); request_ready = 1; }
    else if (zb && z) { snprintf(request_param, 1023, "ZIP:%s", z); request_ready = 1; }
    
    int timeout = 200;
    while(request_ready == 1 && timeout-- > 0) usleep(5000);
    
    struct MHD_Response *r = MHD_create_response_from_buffer(strlen(web_buffer), web_buffer, MHD_RESPMEM_PERSISTENT);
    MHD_add_response_header(r, "Content-Type", "text/html; charset=utf-8");
    enum MHD_Result ret = MHD_queue_response(c, MHD_HTTP_OK, r);
    MHD_destroy_response(r);
    return ret;
}

int start_web() {
    struct MHD_Daemon *d = MHD_start_daemon(MHD_USE_INTERNAL_POLLING_THREAD, 8122, NULL, NULL, &handler, NULL, MHD_OPTION_END);
    return (d != NULL);
}
