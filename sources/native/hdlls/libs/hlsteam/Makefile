LBITS := $(shell getconf LONG_BIT)

UNAME := $(shell uname)

CFLAGS = -Wall -O3 -fPIC -I native/include -I $(HASHLINK_SRC)/src -std=c++0x

ifeq ($(UNAME),Darwin)
    OS=osx
    ARCH=
    RPATH_LFLAG = -Wl,-rpath,@loader_path/.
else
    OS=linux
    ARCH=$(LBITS)
    RPATH_LFLAG = -Wl,-rpath,'$$ORIGIN'
endif

LFLAGS = -lhl -lsteam_api -lstdc++ -L native/lib/$(OS)$(ARCH) $(RPATH_LFLAG)

SRC = native/cloud.o native/common.o native/controller.o native/friends.o native/gameserver.o \
	native/matchmaking.o native/networking.o native/stats.o native/ugc.o

all: ${SRC}
	${CC} ${CFLAGS} -shared -o steam.hdll ${SRC} ${LFLAGS}

install:
	cp steam.hdll /usr/lib
	cp native/lib/$(OS)$(ARCH)/libsteam_api.* /usr/lib
	
.SUFFIXES : .cpp .o

.cpp.o :
	${CC} ${CFLAGS} -o $@ -c $<
	
clean_o:
	rm -f ${SRC}

clean: clean_o
	rm -f steam.hdll

