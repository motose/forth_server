TARGET  = libforth_bridge.so

all: $(TARGET)

$(TARGET):bridge/forth_bridge.c
	gcc -shared -o libforth_bridge.so bridge/forth_bridge.c $(mysql_config --cflags --libs) -lmicrohttpd -fPIC

clean:
	rm -f $(TARGET)
