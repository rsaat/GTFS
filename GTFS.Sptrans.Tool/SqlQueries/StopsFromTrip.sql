SELECT st.trip_id,t.shape_id,st.stop_id, st.stop_sequence,st.departure_time,st.shape_dist_traveled,s.stop_name,s.stop_lat,s.stop_lon from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN stop s ON s.id = st.stop_id 
WHERE ((1=0) OR st.trip_id = "178L-10-1")
ORDER BY  st.trip_id, st.stop_sequence