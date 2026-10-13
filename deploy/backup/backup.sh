#!/bin/sh
# Dumps the game database into /backups every day at BACKUP_TIME (UTC, HH:MM)
# and deletes dumps older than BACKUP_KEEP_DAYS. "backup.sh now" makes one dump and exits.
# Connection settings come from the standard PGHOST, PGUSER, PGPASSWORD and PGDATABASE variables.
set -u
# Dumps hold account data: readable by root on the host only.
umask 077
chmod 700 /backups

log() {
    echo "$(date -u '+%F %T') $*"
}

dump() {
    name="desertod-$(date -u +%Y-%m-%d_%H%M).dump"
    if pg_dump --format=custom --file="/backups/$name.partial"; then
        mv "/backups/$name.partial" "/backups/$name"
        log "wrote $name ($(du -h "/backups/$name" | cut -f1))"
        # Only after a successful dump: if dumps keep failing, old ones must not age out.
        find /backups -name 'desertod-*.dump' -mmin +$((BACKUP_KEEP_DAYS * 1440)) -print -delete
    else
        rm -f "/backups/$name.partial"
        log "backup FAILED" >&2
        return 1
    fi
}

if [ "${1:-}" = "now" ]; then
    dump
    exit $?
fi

if ! date -u -d "today $BACKUP_TIME" +%s > /dev/null 2>&1; then
    log "BACKUP_TIME must be HH:MM, got '$BACKUP_TIME'" >&2
    exit 1
fi

rm -f /backups/*.partial
trap 'exit 0' TERM INT

while true; do
    now=$(date -u +%s)
    next=$(date -u -d "today $BACKUP_TIME" +%s)
    if [ "$next" -le "$now" ]; then
        next=$(date -u -d "tomorrow $BACKUP_TIME" +%s)
    fi
    log "next backup at $(date -u -d "@$next" '+%F %T') UTC"
    # Sleeping in the background lets the TERM trap run immediately on "docker compose down".
    sleep $((next - now)) &
    wait $!
    dump
done
