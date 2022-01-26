#!/bin/bash
#set -ux
#set +e
cd $DEPS_DIR/0/sidecar/
$DEPS_DIR/0/sidecar/KerberosSidecar --urls=http://0.0.0.0:9090 &

#
#declare READINESS_PROBE=http://localhost:9090/health/ready
#declare TIMEOUT=10
#
#LASTSTATUS=000
#
#while (( $TIMEOUT > $SECONDS ))
#do
#    echo 'test'
#    LASTSTATUS=$(curl -s -o /dev/null --connect-timeout 1 -L -w ''%{http_code}'' ${READINESS_PROBE})
#    if [ $LASTSTATUS == 200 ]
#    then
#        exit 0
#    fi
#    sleep 1
#done
#
#if [ $LASTSTATUS == 000 ] ; then
#    >&2 echo "Sidecar failed to startup in allotted time"
#    
#else
#    >&2 echo "Sidecar failed to become healthy"
#fi
#exit 1
