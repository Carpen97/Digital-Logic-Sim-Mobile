`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// By: Yash Chandra Verma (jatinmandav/Verilog-HDL)
// Module: D Latch (level-sensitive) - when CLK=1, C follows A; when CLK=0, C holds
//////////////////////////////////////////////////////////////////////////////////
module d_latch(A, CLK, C);
input A, CLK;
output C;
reg C;

always @(A or CLK)
if (CLK)
C = A;

endmodule
