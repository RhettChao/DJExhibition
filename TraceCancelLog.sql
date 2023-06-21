

2021-12-17 12:33:59.360	
210.69.58.213

SELECT TOP 100 * FROM RequestLog WHERE RequestDate>='2021-12-17 12:33:00' ORDER BY SN
SELECT * FROM AuthLog WHERE AuthDate>='2021-12-17 01:30:00' ORDER BY SN

2021-12-20 08:08:11.063
SELECT TOP 100 * FROM RequestLog WHERE RequestDate>='2021-12-20 08:00:00' ORDER BY SN
SELECT * FROM AuthLog WHERE AuthDate>='2021-12-20 08:00:00' ORDER BY SN

SELECT CreateDate,CancelDate,
VerifyDate,VerifySource,VerifyUserId,VerifyCode,
* FROM Reservation 
WHERE ReservationBegin>='2022-01-19' AND ReservationBegin<'2022-01-20'
--WHERE CancelUserId <>'-1'
ORDER BY SN DESC

SELECT TOP 100 * FROM RequestLog WHERE Path LIKE '%CancelReservation%' ORDER BY SN DESC
