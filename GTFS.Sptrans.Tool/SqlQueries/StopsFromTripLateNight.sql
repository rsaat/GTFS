SELECT DISTINCT t.id, t.trip_headsign, s.stop_name, s.id from trip t 
INNER JOIN frequency f ON t.id=f.trip_id
INNER JOIN stop_time st ON st.trip_id = t.id
INNER JOIN stop s ON st.stop_id = s.id
WHERE t.id LIKE "N%" AND f.start_time LIKE "%"
ORDER BY t.id,f.start_time